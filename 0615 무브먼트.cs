// Movement.cs - 카메라 방향 기준 3인칭 이동, 점프(y축), 회전(이동 방향 바라보기), 네트워크 동기화
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class Movement : MonoBehaviourPunCallbacks, IPunObservable
{
    [Header("컴포넌트 참조")]
    private CharacterController controller;   // 이동 처리용 CharacterController
    private new Transform transform;          // 자체 Transform 참조
    private Animator animator;                // 애니메이션 제어용 Animator
    private Camera mainCamera;                // 메인 카메라 참조

    [Header("이동 설정")]
    [Tooltip("플레이어 이동 속도")]
    public float moveSpeed = 6.0f;            // 카메라 기준 앞으로/옆으로 이동 속도

    [Tooltip("점프 높이")]
    public float jumpForce = 8.0f;            // 점프 시 적용할 초기 수직 속도

    [Tooltip("중력 가속도")]
    public float gravity = 20.0f;             // 중력 가속도 값

    [Header("동기화 민감도")]
    [Tooltip("타 네트워크 플레이어 위치/회전 보간 속도")]
    public float damping = 10.0f;             // 원격 플레이어 위치/회전 보간 속도

    private Vector3 receivePos;               // 네트워크로 받은 원격 플레이어 위치
    private Quaternion receiveRot;            // 네트워크로 받은 원격 플레이어 회전

    // 내부 상태 변수
    private Vector3 velocity = Vector3.zero;  // 수직(y) 속도 포함 전체 속도 벡터
    private bool isJumping = false;           // 점프 중인지 여부

    private AbilityManager abilityManager;


    void Start()
    {
        // 컴포넌트 초기화
        controller = GetComponent<CharacterController>();
        transform = GetComponent<Transform>();
        animator = GetComponent<Animator>();
        mainCamera = Camera.main;

        // 로컬 플레이어라면 카메라 연결
        if (photonView.IsMine)
        {
            CameraController camCtrl = FindObjectOfType<CameraController>();
            if (camCtrl != null)
            {
                camCtrl.target = this.transform;
                Debug.Log("CameraController.target이 로컬 플레이어로 설정됨");
            }
            abilityManager = GetComponent<AbilityManager>();
            if (abilityManager == null)
            {
                Debug.LogError("Movement: AbilityManager 컴포넌트가 플레이어에 없음");
            }
        }

    }

    void Update()
    {
        // 원격 플레이어라면 위치/회전 보간 후 반환
        if (!pv.IsMine)
        {
            transform.position = Vector3.Lerp(transform.position, receivePos, Time.deltaTime * damping);
            transform.rotation = Quaternion.Slerp(transform.rotation, receiveRot, Time.deltaTime * damping);
            return;
        }

        // 로컬 플레이어만 이동 및 입력 처리
        HandleMovement();

        // 능력 입력을 AbilityManager로 위임
        if (abilityManager != null)
        {
            abilityManager.Update();
        }
    }

    // Horizontal 축 입력값 (A/D 또는 좌/우 화살표)
    float h => Input.GetAxis("Horizontal");
    // Vertical 축 입력값 (W/S 또는 앞/뒤 화살표)
    float v => Input.GetAxis("Vertical");

    /// <summary>
    /// 카메라 방향 기준 WASD 이동 및 점프 처리
    /// - W/S: 카메라 전방/후방, A/D: 카메라 좌측/우측
    /// - 점프 시 중력 적용
    /// - 이동 방향(평면)으로 캐릭터 회전
    /// - Animator 파라미터: Forward, Strafe, isJumping
    /// </summary>
    private void HandleMovement()
    {
        // 카메라 전방/우측 벡터를 얻고, 수평 평면으로 제한
        Vector3 camForward = mainCamera.transform.forward;
        Vector3 camRight = mainCamera.transform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        // 입력 기반 이동 벡터: 카메라 기준
        Vector3 moveDir = camForward * v + camRight * h;
        moveDir = moveDir.normalized * moveSpeed;

        //--- Animator 파라미터 업데이트 ---//
        // 1. Forward: W/S 입력값(v)
        animator.SetFloat("Forward", v);

        // 지면 체크
        if (controller.isGrounded)
        {
            // 지면에 있는 동안엔 수직 속도 약간 음수로 고정
            velocity.y = -1f;
            isJumping = false;
            animator.SetBool("IsJumping", false);

            // 스페이스바로 점프
            if (Input.GetButtonDown("Jump"))
            {
                velocity.y = jumpForce;   // 점프 초기 속도 부여
                isJumping = true;
                animator.SetBool("IsJumping", true);
            }
        }
        else
        {
            // 공중에 떠 있으면 중력 가속도 적용
            velocity.y -= gravity * Time.deltaTime;
        }

        // 최종 이동 벡터 = 카메라 기준 평면 이동 + 수직 속도
        Vector3 finalMove = moveDir + Vector3.up * velocity.y;
        controller.Move(finalMove * Time.deltaTime);

        // 평면 이동이 있을 때만 캐릭터 평면 회전
        Vector3 flatMove = new Vector3(moveDir.x, 0f, moveDir.z);
        if (flatMove.sqrMagnitude > 0.01f)
        {
            // 입력 방향으로 캐릭터가 바라보도록 회전 (부드럽게 Slerp)
            Quaternion targetRot = Quaternion.LookRotation(flatMove);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 15f);
        }

        // Animator 파라미터 설정: Forward (Z축 이동값), Strafe (X축 이동값)
        //animator.SetFloat("Forward", v);
        //animator.SetFloat("Strafe", h);
    }

    /// <summary>
    /// 공격/방어/회피 입력 감지
    /// - 왼쪽 마우스 클릭: 공격
    /// - 오른쪽 마우스 클릭: 방어
    /// - LeftShift 키: 회피
    /// ※ 실제 능력 로직은 추후 연결
    /// </summary>
    private void HandleAbilityInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("공격 입력 감지 (추후 Attack() 호출 예정)");
        }

        if (Input.GetMouseButtonDown(1))
        {
            Debug.Log("방어 입력 감지 (추후 Defend() 호출 예정)");
        }

        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            Debug.Log("회피 입력 감지 (추후 Dodge() 호출 예정)");
        }
    }

    /// <summary>
    /// IPunObservable 구현: 위치 및 회전 정보 동기화
    /// - 로컬 플레이어(IsWriting): 위치/회전 전송
    /// - 원격 플레이어: 수신된 값(receivePos, receiveRot)으로 설정
    /// </summary>
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // 내 위치 및 회전 정보를 전송
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
        }
        else
        {
            // 다른 클라이언트의 위치 및 회전 정보를 수신
            receivePos = (Vector3)stream.ReceiveNext();
            receiveRot = (Quaternion)stream.ReceiveNext();
        }
    }

    // PhotonView를 간단히 가져오는 프로퍼티
    private PhotonView pv
    {
        get { return GetComponent<PhotonView>(); }
    }
}
