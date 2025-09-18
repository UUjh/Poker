using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Core;

namespace Network
{
    /// <summary>
    /// 네트워크 관리 매니저 (Singleton)
    /// 게임의 네트워크 세션 및 Relay 서버 관리
    /// </summary>
    public class NetworkManager : Singleton<NetworkManager>
    {
        [Header("Network Settings")]
        [SerializeField] private int maxPlayers = 2;
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private bool autoStartHost = false;
        
        [Header("Relay Settings")]
        [SerializeField] private string region = "asia-northeast1";
        
        // 이벤트
        public static event Action OnHostStarted;
        public static event Action OnClientConnected;
        public static event Action OnClientDisconnected;
        public static event Action<string> OnConnectionFailed;
        public static event Action OnShutdown;
        
        // 상태
        public static bool IsHost { get; private set; } = false;
        public static bool IsClient { get; private set; } = false;
        public static bool IsConnected { get; private set; } = false;
        public static string JoinCode { get; private set; } = "";
        public static int ConnectedPlayers { get; private set; } = 0;
        
        // Relay 관련
        private Allocation _allocation;
        private JoinAllocation _joinAllocation;
        
        protected override void OnSingletonAwake()
        {
            base.OnSingletonAwake();
            
            // Unity Netcode 이벤트 구독
            Unity.Netcode.NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
            Unity.Netcode.NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectedCallback;
            Unity.Netcode.NetworkManager.Singleton.OnServerStarted += OnServerStartedCallback;
            
            LogDebug("NetworkManager 초기화 완료");
        }
        
        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            // Unity Netcode 이벤트 구독 해제
            if (Unity.Netcode.NetworkManager.Singleton != null)
            {
                Unity.Netcode.NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;
                Unity.Netcode.NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectedCallback;
                Unity.Netcode.NetworkManager.Singleton.OnServerStarted -= OnServerStartedCallback;
            }
        }
        
        /// <summary>
        /// Relay 서버 생성 및 호스트 시작
        /// </summary>
        public async Task<bool> StartHost()
        {
            if (IsConnected)
            {
                LogWarning("이미 연결되어 있습니다.");
                return false;
            }
            
            try
            {
                LogDebug("Relay 서버 생성 중...");
                
                // Relay 서버 생성
                _allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1, region);
                JoinCode = await RelayService.Instance.GetJoinCodeAsync(_allocation.AllocationId);
                
                LogDebug($"Relay 서버 생성 완료 - Join Code: {JoinCode}");
                
                // Unity Transport 설정
                var transport = Unity.Netcode.NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
                transport.SetRelayServerData(_allocation.RelayServer.IpV4, (ushort)_allocation.RelayServer.Port, 
                    _allocation.AllocationIdBytes, _allocation.Key, _allocation.ConnectionData);
                
                // 호스트 시작
                bool success = Unity.Netcode.NetworkManager.Singleton.StartHost();
                
                if (success)
                {
                    IsHost = true;
                    IsConnected = true;
                    ConnectedPlayers = 1;
                    
                    LogDebug("호스트 시작 완료");
                    OnHostStarted?.Invoke();
                }
                else
                {
                    LogError("호스트 시작 실패");
                    OnConnectionFailed?.Invoke("호스트 시작 실패");
                }
                
                return success;
            }
            catch (Exception e)
            {
                string errorMessage = $"호스트 시작 실패: {e.Message}";
                LogError(errorMessage);
                OnConnectionFailed?.Invoke(errorMessage);
                return false;
            }
        }
        
        /// <summary>
        /// Relay 서버 참여 및 클라이언트 시작
        /// </summary>
        public async Task<bool> StartClient(string joinCode)
        {
            if (IsConnected)
            {
                LogWarning("이미 연결되어 있습니다.");
                return false;
            }
            
            if (string.IsNullOrEmpty(joinCode))
            {
                LogError("Join Code가 비어있습니다.");
                return false;
            }
            
            try
            {
                LogDebug($"Relay 서버 참여 중... Join Code: {joinCode}");
                
                // Relay 서버 참여
                _joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
                
                LogDebug("Relay 서버 참여 완료");
                
                // Unity Transport 설정
                var transport = Unity.Netcode.NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
                transport.SetRelayServerData(_joinAllocation.RelayServer.IpV4, (ushort)_joinAllocation.RelayServer.Port, 
                    _joinAllocation.AllocationIdBytes, _joinAllocation.Key, _joinAllocation.ConnectionData, 
                    _joinAllocation.HostConnectionData);
                
                // 클라이언트 시작
                bool success = Unity.Netcode.NetworkManager.Singleton.StartClient();
                
                if (success)
                {
                    IsClient = true;
                    JoinCode = joinCode;
                    
                    LogDebug("클라이언트 시작 완료");
                }
                else
                {
                    LogError("클라이언트 시작 실패");
                    OnConnectionFailed?.Invoke("클라이언트 시작 실패");
                }
                
                return success;
            }
            catch (Exception e)
            {
                string errorMessage = $"클라이언트 시작 실패: {e.Message}";
                LogError(errorMessage);
                OnConnectionFailed?.Invoke(errorMessage);
                return false;
            }
        }
        
        /// <summary>
        /// 네트워크 연결 종료
        /// </summary>
        public void Shutdown()
        {
            if (!IsConnected)
            {
                LogWarning("연결되어 있지 않습니다.");
                return;
            }
            
            LogDebug("네트워크 연결 종료 중...");
            
            Unity.Netcode.NetworkManager.Singleton.Shutdown();
            
            IsHost = false;
            IsClient = false;
            IsConnected = false;
            JoinCode = "";
            ConnectedPlayers = 0;
            
            LogDebug("네트워크 연결 종료 완료");
            OnShutdown?.Invoke();
        }
        
        /// <summary>
        /// 서버 시작 콜백
        /// </summary>
        private void OnServerStartedCallback()
        {
            LogDebug("서버 시작됨");
        }
        
        /// <summary>
        /// 클라이언트 연결 콜백
        /// </summary>
        private void OnClientConnectedCallback(ulong clientId)
        {
            ConnectedPlayers++;
            IsConnected = true;
            
            LogDebug($"클라이언트 연결됨 - ID: {clientId}, 총 연결 수: {ConnectedPlayers}");
            OnClientConnected?.Invoke();
        }
        
        /// <summary>
        /// 클라이언트 연결 해제 콜백
        /// </summary>
        private void OnClientDisconnectedCallback(ulong clientId)
        {
            ConnectedPlayers--;
            
            LogDebug($"클라이언트 연결 해제됨 - ID: {clientId}, 총 연결 수: {ConnectedPlayers}");
            OnClientDisconnected?.Invoke();
        }
        
        /// <summary>
        /// 네트워크 상태 확인
        /// </summary>
        public void CheckNetworkStatus()
        {
            LogDebug("=== 네트워크 상태 ===");
            LogDebug($"호스트: {IsHost}");
            LogDebug($"클라이언트: {IsClient}");
            LogDebug($"연결됨: {IsConnected}");
            LogDebug($"Join Code: {JoinCode}");
            LogDebug($"연결된 플레이어 수: {ConnectedPlayers}");
            LogDebug($"Netcode 상태: {Unity.Netcode.NetworkManager.Singleton.IsHost} / {Unity.Netcode.NetworkManager.Singleton.IsClient}");
            LogDebug("==================");
        }
        
        /// <summary>
        /// 디버그 로그 출력
        /// </summary>
        private void LogDebug(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[NetworkManager] {message}");
            }
        }
        
        /// <summary>
        /// 경고 로그 출력
        /// </summary>
        private void LogWarning(string message)
        {
            Debug.LogWarning($"[NetworkManager] {message}");
        }
        
        /// <summary>
        /// 에러 로그 출력
        /// </summary>
        private void LogError(string message)
        {
            Debug.LogError($"[NetworkManager] {message}");
        }
    }
}
