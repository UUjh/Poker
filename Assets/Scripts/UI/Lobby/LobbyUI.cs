using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Network;

namespace Poker.UI.Lobby
{
    /// <summary>
    /// 로비 UI 컨트롤러
    /// 게임의 로비 화면 UI 관리
    /// </summary>
    public class LobbyUI : MonoBehaviour
    {
        [Header("Main Buttons")]
        [SerializeField] private Button createRoomBtn;
        [SerializeField] private Button joinRoomBtn;
        [SerializeField] private Button roomListBtn;
        [SerializeField] private Button quickMatchBtn;
        [SerializeField] private Button exitBtn;
        
        [Header("Room Creation")]
        [SerializeField] private GameObject createRoomPanel;
        [SerializeField] private TMP_InputField roomNameInput;
        [SerializeField] private Toggle privateToggle;
        [SerializeField] private TMP_InputField roomPasswordInput;
        [SerializeField] private Button createRoomCreateBtn;
        [SerializeField] private Button createRoomCancelBtn;
        
        [Header("Room Joining")]
        [SerializeField] private GameObject joinRoomPanel;
        [SerializeField] private TMP_InputField joinCodeInputField;
        [SerializeField] private Button joinRoomConfirmBtn;
        [SerializeField] private Button joinRoomCancelBtn;
        
        [Header("Player Info")]
        [SerializeField] private TextMeshProUGUI playerNameText;
        [SerializeField] private TextMeshProUGUI playerIdText;
        
        [Header("Status")]
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private GameObject loadingPanel;
        
        private void Start()
        {
            InitUI();
            SubscribeEvents();
        }
        
        private void OnDestroy()
        {
            UnsubscribeEvents();
        }
        
        /// <summary>
        /// UI 초기화
        /// </summary>
        private void InitUI()
        {
            // 버튼 이벤트 연결
            if (createRoomBtn != null) createRoomBtn.onClick.AddListener(OnCreateRoomClicked);
            if (joinRoomBtn != null) joinRoomBtn.onClick.AddListener(OnJoinRoomClicked);
            if (roomListBtn != null) roomListBtn.onClick.AddListener(OnRoomListClicked);
            if (quickMatchBtn != null) quickMatchBtn.onClick.AddListener(OnQuickMatchClicked);
            if (exitBtn != null) exitBtn.onClick.AddListener(OnExitClicked);
            
            // 방 생성 패널 버튼
            if (createRoomCreateBtn != null) createRoomCreateBtn.onClick.AddListener(OnCreateRoomConfirmClicked);
            if (createRoomCancelBtn != null) createRoomCancelBtn.onClick.AddListener(OnCreateRoomCancelClicked);
            if (privateToggle != null) privateToggle.onValueChanged.AddListener(OnPrivateToggleChanged);
            
            // 방 참여 패널 버튼
            if (joinRoomConfirmBtn != null) joinRoomConfirmBtn.onClick.AddListener(OnJoinRoomConfirmClicked);
            if (joinRoomCancelBtn != null) joinRoomCancelBtn.onClick.AddListener(OnJoinRoomCancelClicked);
            
            // 패널 초기화
            HideAllPanels();
            if (loadingPanel != null) loadingPanel.SetActive(false);
            // 비공개 토글 UI 초기화
            ApplyPrivateToggleUI();
            
            // 플레이어 정보 업데이트
            UpdatePlayerInfo();
            
            // 상태 텍스트 초기화
            UpdateStatusText("로비에 접속했습니다.");
        }
        
        /// <summary>
        /// 이벤트 구독
        /// </summary>
        private void SubscribeEvents()
        {
            LobbyManager.OnLobbyInitialized += OnLobbyInitialized;
            LobbyManager.OnRoomCreated += OnRoomCreated;
            LobbyManager.OnRoomJoined += OnRoomJoined;
            LobbyManager.OnQuickMatchStarted += OnQuickMatchStarted;
            LobbyManager.OnLobbyError += OnLobbyError;
            AuthenticationManager.OnPlayerNameChanged += OnPlayerNameChanged;
        }
        
        /// <summary>
        /// 이벤트 구독 해제
        /// </summary>
        private void UnsubscribeEvents()
        {
            LobbyManager.OnLobbyInitialized -= OnLobbyInitialized;
            LobbyManager.OnRoomCreated -= OnRoomCreated;
            LobbyManager.OnRoomJoined -= OnRoomJoined;
            LobbyManager.OnQuickMatchStarted -= OnQuickMatchStarted;
            LobbyManager.OnLobbyError -= OnLobbyError;
            AuthenticationManager.OnPlayerNameChanged -= OnPlayerNameChanged;
        }
        
        /// <summary>
        /// 방 생성 버튼 클릭
        /// </summary>
        private void OnCreateRoomClicked()
        {
            Debug.Log("방 생성 버튼 클릭");
            HideAllPanels();
            if (createRoomPanel != null) createRoomPanel.SetActive(true);
            ApplyPrivateToggleUI();
        }
        
        /// <summary>
        /// 방 참여 버튼 클릭
        /// </summary>
        private void OnJoinRoomClicked()
        {
            Debug.Log("방 참여 버튼 클릭");
            HideAllPanels();
            if (joinRoomPanel != null) joinRoomPanel.SetActive(true);
        }
        
        /// <summary>
        /// 방 목록 버튼 클릭
        /// </summary>
        private void OnRoomListClicked()
        {
            Debug.Log("방 목록 버튼 클릭");
            UpdateStatusText("방 목록은 추후 제공됩니다.");
        }
        
        /// <summary>
        /// 퀵 매치 버튼 클릭
        /// </summary>
        private void OnQuickMatchClicked()
        {
            Debug.Log("퀵 매치 버튼 클릭");
            ShowLoading("퀵 매치 시작 중...");
            LobbyManager.Instance.StartQuickMatchAsync();
        }
        
        /// <summary>
        /// 게임 종료 버튼 클릭
        /// </summary>
        private void OnExitClicked()
        {
            Debug.Log("게임 종료 버튼 클릭");
            Application.Quit();
        }
        
        /// <summary>
        /// 방 생성 확인 버튼 클릭
        /// </summary>
        private void OnCreateRoomConfirmClicked()
        {
            Debug.Log("방 생성 확인 버튼 클릭");
            ShowLoading("방 생성 중...");
            // TODO: 패스워드는 네트워크 계층으로 전달하여 검증/저장 예정
            string roomName = roomNameInput != null ? roomNameInput.text : string.Empty;
            bool isPrivate = privateToggle != null && privateToggle.isOn;
            string password = (isPrivate && roomPasswordInput != null)
                ? roomPasswordInput.text
                : string.Empty;
            // 현재는 UI 단계까지만 처리. 필요 시 LobbyManager에 전달하도록 확장
            LobbyManager.Instance.CreateRoomAsync(roomName, isPrivate, password);
        }
        
        /// <summary>
        /// 방 생성 취소 버튼 클릭
        /// </summary>
        private void OnCreateRoomCancelClicked()
        {
            Debug.Log("방 생성 취소 버튼 클릭");
            HideAllPanels();
        }
        
        /// <summary>
        /// 방 참여 확인 버튼 클릭
        /// </summary>
        private void OnJoinRoomConfirmClicked()
        {
            string joinCode = joinCodeInputField != null ? joinCodeInputField.text : "";
            Debug.Log($"방 참여 확인 버튼 클릭 - Join Code: {joinCode}");
            
            if (string.IsNullOrEmpty(joinCode))
            {
                UpdateStatusText("Join Code를 입력해주세요.");
                return;
            }
            
            ShowLoading("방 참여 중...");
            LobbyManager.Instance.JoinRoomAsync(joinCode);
        }
        
        /// <summary>
        /// 방 참여 취소 버튼 클릭
        /// </summary>
        private void OnJoinRoomCancelClicked()
        {
            Debug.Log("방 참여 취소 버튼 클릭");
            HideAllPanels();
        }
        
        /// <summary>
        /// 플레이어 정보 업데이트
        /// </summary>
        private void UpdatePlayerInfo()
        {
            if (playerNameText != null)
            {
                playerNameText.text = $"플레이어: {AuthenticationManager.PlayerName}";
            }
            
            if (playerIdText != null)
            {
                playerIdText.text = $"ID: {AuthenticationManager.PlayerId}";
            }
        }
        
        /// <summary>
        /// 상태 텍스트 업데이트
        /// </summary>
        private void UpdateStatusText(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }
        
        /// <summary>
        /// 로딩 패널 표시
        /// </summary>
        private void ShowLoading(string message)
        {
            if (loadingPanel != null) loadingPanel.SetActive(true);
            UpdateStatusText(message);
        }
        
        /// <summary>
        /// 로딩 패널 숨기기
        /// </summary>
        private void HideLoading()
        {
            if (loadingPanel != null) loadingPanel.SetActive(false);
        }

        /// <summary>
        /// 모든 패널 숨김 처리
        /// </summary>
        private void HideAllPanels()
        {
            if (createRoomPanel != null) createRoomPanel.SetActive(false);
            if (joinRoomPanel != null) joinRoomPanel.SetActive(false);
        }

        /// <summary>
        /// 비공개 방 토글 변경 시 UI 적용
        /// </summary>
        private void OnPrivateToggleChanged(bool isOn)
        {
            ApplyPrivateToggleUI();
        }

        /// <summary>
        /// 비공개 토글 상태에 따라 패스워드 입력 활성/비활성
        /// </summary>
        private void ApplyPrivateToggleUI()
        {
            bool enable = privateToggle != null && privateToggle.isOn;
            if (roomPasswordInput != null)
            {
                roomPasswordInput.gameObject.SetActive(enable);
                roomPasswordInput.interactable = enable;
                // 시각적으로 패스워드 입력처럼 보이도록 설정 (선택 사항)
                roomPasswordInput.contentType = enable ? TMP_InputField.ContentType.Password : TMP_InputField.ContentType.Standard;
                if (!enable)
                {
                    roomPasswordInput.text = string.Empty;
                }
            }
        }
        
        /// <summary>
        /// 로비 초기화 완료 콜백
        /// </summary>
        private void OnLobbyInitialized()
        {
            Debug.Log("로비 초기화 완료");
            UpdateStatusText("로비가 준비되었습니다.");
            HideLoading();
        }
        
        /// <summary>
        /// 방 생성 완료 콜백
        /// </summary>
        private void OnRoomCreated(string joinCode)
        {
            Debug.Log($"방 생성 완료 - Join Code: {joinCode}");
            UpdateStatusText($"방이 생성되었습니다. Join Code: {joinCode}");
        }
        
        /// <summary>
        /// 방 참여 완료 콜백
        /// </summary>
        private void OnRoomJoined(string joinCode)
        {
            Debug.Log($"방 참여 완료 - Join Code: {joinCode}");
            UpdateStatusText("방에 참여했습니다.");
        }
        
        /// <summary>
        /// 퀵 매치 시작 콜백
        /// </summary>
        private void OnQuickMatchStarted()
        {
            Debug.Log("퀵 매치 시작");
            UpdateStatusText("퀵 매치가 시작되었습니다.");
        }
        
        /// <summary>
        /// 로비 에러 콜백
        /// </summary>
        private void OnLobbyError(string error)
        {
            Debug.LogError($"로비 에러: {error}");
            UpdateStatusText($"오류: {error}");
            HideLoading();
        }
        
        /// <summary>
        /// 플레이어 이름 변경 콜백
        /// </summary>
        private void OnPlayerNameChanged(string newName)
        {
            Debug.Log($"플레이어 이름 변경: {newName}");
            UpdatePlayerInfo();
        }
    }
}
