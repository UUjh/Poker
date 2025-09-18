using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Core;
using Network;

namespace Poker.UI.Lobby
{
    /// <summary>
    /// 로비 관리 매니저 (Singleton)
    /// 게임의 로비 화면 및 방 관리
    /// </summary>
    public class LobbyManager : Singleton<LobbyManager>
    {
        [Header("Lobby Settings")]
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private string gameSceneName = "Game";
        
        [Header("UI References")]
        [SerializeField] private GameObject createRoomPanel;
        [SerializeField] private GameObject joinRoomPanel;
        [SerializeField] private GameObject roomListPanel;
        [SerializeField] private GameObject quickMatchPanel;
        
        // 이벤트
        public static event Action OnLobbyInitialized;
        public static event Action<string> OnRoomCreated;
        public static event Action<string> OnRoomJoined;
        public static event Action OnQuickMatchStarted;
        public static event Action<string> OnLobbyError;
        
        // 상태
        public static bool IsLobbyReady { get; private set; } = false;
        public static string CurJoinCode { get; private set; } = "";
        
        protected override void OnSingletonAwake()
        {
            base.OnSingletonAwake();
            
            // 네트워크 이벤트 구독
            NetworkManager.OnHostStarted += OnHostStarted;
            NetworkManager.OnClientConnected += OnClientConnected;
            NetworkManager.OnConnectionFailed += OnConnectionFailed;
            AuthenticationManager.OnAuthenticationSuccess += OnAuthenticationSuccess;
            
            LogDebug("LobbyManager 초기화 완료");
        }
        
        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            // 네트워크 이벤트 구독 해제
            NetworkManager.OnHostStarted -= OnHostStarted;
            NetworkManager.OnClientConnected -= OnClientConnected;
            NetworkManager.OnConnectionFailed -= OnConnectionFailed;
            AuthenticationManager.OnAuthenticationSuccess -= OnAuthenticationSuccess;
        }
        
        /// <summary>
        /// 로비 초기화
        /// </summary>
        public void InitLobby()
        {
            LogDebug("로비 초기화 시작...");
            
            // 인증 상태 확인
            if (!AuthenticationManager.Instance.CheckAuthenticationStatus())
            {
                LogError("인증이 필요합니다.");
                OnLobbyError?.Invoke("인증이 필요합니다.");
                return;
            }
            
            // UI 초기화
            InitUI();
            
            IsLobbyReady = true;
            LogDebug("로비 초기화 완료");
            OnLobbyInitialized?.Invoke();
        }
        
        /// <summary>
        /// UI 초기화
        /// </summary>
        private void InitUI()
        {
            HideAllPanels();
            
            LogDebug("UI 초기화 완료");
        }
        
        /// <summary>
        /// 모든 패널 숨김 처리
        /// </summary>
        private void HideAllPanels()
        {
            if (createRoomPanel != null) createRoomPanel.SetActive(false);
            if (joinRoomPanel != null) joinRoomPanel.SetActive(false);
            if (roomListPanel != null) roomListPanel.SetActive(false);
            if (quickMatchPanel != null) quickMatchPanel.SetActive(false);
        }
        
        /// <summary>
        /// 방 생성
        /// </summary>
        public async void CreateRoomAsync()
        {
            if (!IsLobbyReady)
            {
                LogError("로비가 준비되지 않았습니다.");
                return;
            }
            
            LogDebug("방 생성 시작...");
            
            try
            {
                // 호스트 시작
                bool success = await NetworkManager.Instance.StartHost();
                
                if (success)
                {
                    CurJoinCode = NetworkManager.JoinCode;
                    LogDebug($"방 생성 완료 - Join Code: {CurJoinCode}");
                    OnRoomCreated?.Invoke(CurJoinCode);
                    
                    // 게임 씬으로 이동
                    LoadGameScene();
                }
                else
                {
                    LogError("방 생성 실패");
                    OnLobbyError?.Invoke("방 생성에 실패했습니다.");
                }
            }
            catch (Exception e)
            {
                string errorMessage = $"방 생성 중 오류 발생: {e.Message}";
                LogError(errorMessage);
                OnLobbyError?.Invoke(errorMessage);
            }
        }
        
        /// <summary>
        /// 방 참여
        /// </summary>
        public async void JoinRoomAsync(string joinCode)
        {
            if (!IsLobbyReady)
            {
                LogError("로비가 준비되지 않았습니다.");
                return;
            }
            
            if (string.IsNullOrEmpty(joinCode))
            {
                LogError("Join Code가 비어있습니다.");
                OnLobbyError?.Invoke("Join Code를 입력해주세요.");
                return;
            }
            
            LogDebug($"방 참여 시작... Join Code: {joinCode}");
            
            try
            {
                // 클라이언트 시작
                bool success = await NetworkManager.Instance.StartClient(joinCode);
                
                if (success)
                {
                    CurJoinCode = joinCode;
                    LogDebug("방 참여 완료");
                    OnRoomJoined?.Invoke(joinCode);
                    // 클라이언트는 서버의 씬 전환을 기다립니다
                }
                else
                {
                    LogError("방 참여 실패");
                    OnLobbyError?.Invoke("방 참여에 실패했습니다.");
                }
            }
            catch (Exception e)
            {
                string errorMessage = $"방 참여 중 오류 발생: {e.Message}";
                LogError(errorMessage);
                OnLobbyError?.Invoke(errorMessage);
            }
        }
        
        /// <summary>
        /// 퀵 매치 시작
        /// </summary>
        public async void StartQuickMatchAsync()
        {
            if (!IsLobbyReady)
            {
                LogError("로비가 준비되지 않았습니다.");
                return;
            }
            
            LogDebug("퀵 매치 시작...");
            
            try
            {
                // 자동으로 방 생성 (퀵 매치는 호스트가 됨)
                bool success = await NetworkManager.Instance.StartHost();
                
                if (success)
                {
                    CurJoinCode = NetworkManager.JoinCode;
                    LogDebug($"퀵 매치 시작 완료 - Join Code: {CurJoinCode}");
                    OnQuickMatchStarted?.Invoke();
                    
                    // 게임 씬으로 이동
                    LoadGameScene();
                }
                else
                {
                    LogError("퀵 매치 시작 실패");
                    OnLobbyError?.Invoke("퀵 매치 시작에 실패했습니다.");
                }
            }
            catch (Exception e)
            {
                string errorMessage = $"퀵 매치 시작 중 오류 발생: {e.Message}";
                LogError(errorMessage);
                OnLobbyError?.Invoke(errorMessage);
            }
        }
        
        /// <summary>
        /// 게임 씬 로드
        /// </summary>
        private void LoadGameScene()
        {
            LogDebug($"게임 씬 로드 요청... {gameSceneName}");
            var netManager = Unity.Netcode.NetworkManager.Singleton;
            if (netManager != null && netManager.IsServer)
            {
                netManager.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
                LogDebug("서버가 씬 전환을 시작했습니다. 클라이언트는 자동 동기화됩니다.");
            }
            else
            {
                LogDebug("클라이언트는 서버의 씬 전환을 대기합니다.");
            }
        }
        
        /// <summary>
        /// 방 목록 표시
        /// </summary>
        public void ShowRoomList()
        {
            LogDebug("방 목록 표시");
            HideAllPanels();
            if (roomListPanel != null) roomListPanel.SetActive(true);
        }
        
        /// <summary>
        /// 방 생성 패널 표시
        /// </summary>
        public void ShowCreateRoomPanel()
        {
            LogDebug("방 생성 패널 표시");
            HideAllPanels();
            if (createRoomPanel != null) createRoomPanel.SetActive(true);
        }
        
        /// <summary>
        /// 방 참여 패널 표시
        /// </summary>
        public void ShowJoinRoomPanel()
        {
            LogDebug("방 참여 패널 표시");
            HideAllPanels();
            if (joinRoomPanel != null) joinRoomPanel.SetActive(true);
        }
        
        /// <summary>
        /// 퀵 매치 패널 표시
        /// </summary>
        public void ShowQuickMatchPanel()
        {
            LogDebug("퀵 매치 패널 표시");
            HideAllPanels();
            if (quickMatchPanel != null) quickMatchPanel.SetActive(true);
        }
        
        /// <summary>
        /// 호스트 시작 콜백
        /// </summary>
        private void OnHostStarted()
        {
            LogDebug("호스트 시작됨");
        }
        
        /// <summary>
        /// 클라이언트 연결 콜백
        /// </summary>
        private void OnClientConnected()
        {
            LogDebug("클라이언트 연결됨");
        }
        
        /// <summary>
        /// 연결 실패 콜백
        /// </summary>
        private void OnConnectionFailed(string error)
        {
            LogError($"연결 실패: {error}");
            OnLobbyError?.Invoke(error);
        }
        
        /// <summary>
        /// 인증 성공 콜백
        /// </summary>
        private void OnAuthenticationSuccess()
        {
            LogDebug("인증 성공 - 로비 초기화");
            InitLobby();
        }
        
        /// <summary>
        /// 디버그 로그 출력
        /// </summary>
        private void LogDebug(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[LobbyManager] {message}");
            }
        }
        
        /// <summary>
        /// 에러 로그 출력
        /// </summary>
        private void LogError(string message)
        {
            Debug.LogError($"[LobbyManager] {message}");
        }
    }
}
