using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Multiplayer;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

namespace Network
{
    /// <summary>
    /// Unity Services 초기화 및 관리 클래스
    /// 네트워크 서비스 설정
    /// </summary>
    public class UgsInitializer : MonoBehaviour
    {
        [Header("Unity Services Settings")]
        [SerializeField] private bool initOnStart = true;
        [SerializeField] private bool enableDebugLogs = true;
        
        [Header("Player Settings")]
        [SerializeField] private string defPlayerName = "Player";
        [SerializeField] private int maxPlayers = 2;
        
        // 이벤트
        public static event Action OnInitComplete;
        public static event Action<string> OnInitFailed;
        public static event Action<string> OnPlayerNameSet;
        
        // 상태
        public static bool IsInit { get; private set; } = false;
        public static string PlayerName { get; private set; } = "";
        public static string PlayerId { get; private set; } = "";
        
        private void Start()
        {
            if (initOnStart)
            {
                InitUgs();
            }
        }
        
        /// <summary>
        /// Unity Services 초기화
        /// </summary>
        public async void InitUgs()
        {
            try
            {
                LogDebug("Unity Services 초기화 시작...");
                
                // 1. Unity Services Core 초기화
                await InitCore();
                
                // 2. Authentication 초기화
                await InitAuthentication();
                
                // 3. Multiplayer 초기화
                await InitMultiplayer();
                
                // 4. 플레이어 이름 설정
                await SetPlayerNameAsync();
                
                IsInit = true;
                LogDebug("Unity Services 초기화 완료!");
                OnInitComplete?.Invoke();
            }
            catch (Exception e)
            {
                string errorMessage = $"Unity Services 초기화 실패: {e.Message}";
                LogError(errorMessage);
                OnInitFailed?.Invoke(errorMessage);
            }
        }
        
        /// <summary>
        /// Unity Services Core 초기화
        /// </summary>
        private async Task InitCore()
        {
            LogDebug("Unity Services Core 초기화 중...");
            
            var options = new InitializationOptions();
            await UnityServices.InitializeAsync(options);
            
            LogDebug("Unity Services Core 초기화 완료");
        }
        
        /// <summary>
        /// Authentication 서비스 초기화
        /// </summary>
        private async Task InitAuthentication()
        {
            LogDebug("Authentication 서비스 초기화 중...");
            
            // 익명 로그인
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            
            PlayerId = AuthenticationService.Instance.PlayerId;
            LogDebug($"익명 로그인 완료 - Player ID: {PlayerId}");
        }
        
        /// <summary>
        /// Multiplayer 서비스 초기화
        /// </summary>
        private Task InitMultiplayer()
        {
            LogDebug("Multiplayer 서비스 초기화 중...");
            
            // Multiplayer 서비스는 자동으로 초기화됨
            // 별도의 Initialize 호출이 필요하지 않음
            
            LogDebug("Multiplayer 서비스 초기화 완료");
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// 플레이어 이름 설정
        /// </summary>
        private async Task SetPlayerNameAsync()
        {
            LogDebug("플레이어 이름 설정 중...");
            
            // 기본 이름 + 랜덤 숫자로 고유한 이름 생성
            string playerName = $"{defPlayerName}_{UnityEngine.Random.Range(1000, 9999)}";
            
            // Authentication에 플레이어 이름 설정
            await AuthenticationService.Instance.UpdatePlayerNameAsync(playerName);
            
            PlayerName = playerName;
            LogDebug($"플레이어 이름 설정 완료: {PlayerName}");
            OnPlayerNameSet?.Invoke(PlayerName);
        }
        
        /// <summary>
        /// 플레이어 이름 변경
        /// </summary>
        public async Task<bool> ChangePlayerNameAsync(string newName)
        {
            try
            {
                if (string.IsNullOrEmpty(newName))
                {
                    LogError("플레이어 이름이 비어있습니다.");
                    return false;
                }
                
                LogDebug($"플레이어 이름 변경 중: {PlayerName} -> {newName}");
                
                await AuthenticationService.Instance.UpdatePlayerNameAsync(newName);
                PlayerName = newName;
                
                LogDebug($"플레이어 이름 변경 완료: {PlayerName}");
                OnPlayerNameSet?.Invoke(PlayerName);
                
                return true;
            }
            catch (Exception e)
            {
                LogError($"플레이어 이름 변경 실패: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Relay 서버 생성
        /// </summary>
        public async Task<string> CreateRelayServerAsync()
        {
            try
            {
                LogDebug("Relay 서버 생성 중...");
                
                var allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1);
                var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
                
                LogDebug($"Relay 서버 생성 완료 - Join Code: {joinCode}");
                
                return joinCode;
            }
            catch (Exception e)
            {
                LogError($"Relay 서버 생성 실패: {e.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Relay 서버 참여
        /// </summary>
        public async Task<bool> JoinRelayServerAsync(string joinCode)
        {
            try
            {
                LogDebug($"Relay 서버 참여 중... Join Code: {joinCode}");
                
                var joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
                
                LogDebug("Relay 서버 참여 완료");
                return true;
            }
            catch (Exception e)
            {
                LogError($"Relay 서버 참여 실패: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 디버그 로그 출력
        /// </summary>
        private void LogDebug(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[UgsInitializer] {message}");
            }
        }
        
        /// <summary>
        /// 에러 로그 출력
        /// </summary>
        private void LogError(string message)
        {
            Debug.LogError($"[UgsInitializer] {message}");
        }
        
        /// <summary>
        /// 서비스 상태 확인
        /// </summary>
        public void CheckServiceStatus()
        {
            LogDebug("=== Unity Services 상태 ===");
            LogDebug($"초기화 상태: {IsInit}");
            LogDebug($"플레이어 ID: {PlayerId}");
            LogDebug($"플레이어 이름: {PlayerName}");
            LogDebug($"인증 상태: {AuthenticationService.Instance.IsSignedIn}");
            LogDebug("========================");
        }
    }
}
