using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Authentication;
using Core;

namespace Network
{
    /// <summary>
    /// 인증 관리 매니저 (Singleton)
    /// 게임의 사용자 인증 및 플레이어 정보 관리
    /// </summary>
    public class AuthenticationManager : Singleton<AuthenticationManager>
    {
        [Header("Player Settings")]
        [SerializeField] private string defaultPlayerName = "Player";
        [SerializeField] private bool enableDebugLogs = true;
        
        // 이벤트
        public static event Action OnAuthenticationSuccess;
        public static event Action<string> OnAuthenticationFailed;
        public static event Action<string> OnPlayerNameChanged;
        public static event Action OnSignOut;
        
        // 상태
        public static bool IsAuthenticated { get; private set; } = false;
        public static string PlayerName { get; private set; } = "";
        public static string PlayerId { get; private set; } = "";
        public static bool IsSigningIn { get; private set; } = false;
        
        protected override void OnSingletonAwake()
        {
            base.OnSingletonAwake();
            LogDebug("AuthenticationManager 초기화 완료");
        }
        
        /// <summary>
        /// 익명 로그인
        /// </summary>
        public async Task<bool> SignInAnonymously()
        {
            if (IsSigningIn)
            {
                LogWarning("이미 로그인 중입니다.");
                return false;
            }
            
            if (IsAuthenticated)
            {
                LogWarning("이미 로그인되어 있습니다.");
                return true;
            }
            
            try
            {
                IsSigningIn = true;
                LogDebug("익명 로그인 시작...");
                
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                
                PlayerId = AuthenticationService.Instance.PlayerId;
                PlayerName = AuthenticationService.Instance.PlayerName;
                
                // 플레이어 이름이 비어있으면 기본 이름 설정
                if (string.IsNullOrEmpty(PlayerName))
                {
                    await SetPlayerName(defaultPlayerName);
                }
                
                IsAuthenticated = true;
                IsSigningIn = false;
                
                LogDebug($"익명 로그인 성공 - Player ID: {PlayerId}, Name: {PlayerName}");
                OnAuthenticationSuccess?.Invoke();
                
                return true;
            }
            catch (Exception e)
            {
                IsSigningIn = false;
                string errorMessage = $"익명 로그인 실패: {e.Message}";
                LogError(errorMessage);
                OnAuthenticationFailed?.Invoke(errorMessage);
                return false;
            }
        }
        
        /// <summary>
        /// 플레이어 이름 설정
        /// </summary>
        public async Task<bool> SetPlayerName(string newName)
        {
            if (!IsAuthenticated)
            {
                LogError("로그인이 필요합니다.");
                return false;
            }
            
            if (string.IsNullOrEmpty(newName))
            {
                LogError("플레이어 이름이 비어있습니다.");
                return false;
            }
            
            try
            {
                LogDebug($"플레이어 이름 설정 중: {PlayerName} -> {newName}");
                
                await AuthenticationService.Instance.UpdatePlayerNameAsync(newName);
                PlayerName = newName;
                
                LogDebug($"플레이어 이름 설정 완료: {PlayerName}");
                OnPlayerNameChanged?.Invoke(PlayerName);
                
                return true;
            }
            catch (Exception e)
            {
                string errorMessage = $"플레이어 이름 설정 실패: {e.Message}";
                LogError(errorMessage);
                return false;
            }
        }
        
        /// <summary>
        /// 로그아웃
        /// </summary>
        public async Task<bool> SignOut()
        {
            if (!IsAuthenticated)
            {
                LogWarning("이미 로그아웃되어 있습니다.");
                return true;
            }
            
            try
            {
                LogDebug("로그아웃 시작...");
                
                AuthenticationService.Instance.SignOut();
                
                IsAuthenticated = false;
                PlayerId = "";
                PlayerName = "";
                
                LogDebug("로그아웃 완료");
                OnSignOut?.Invoke();
                
                return true;
            }
            catch (Exception e)
            {
                string errorMessage = $"로그아웃 실패: {e.Message}";
                LogError(errorMessage);
                return false;
            }
        }
        
        /// <summary>
        /// 인증 상태 확인
        /// </summary>
        public bool CheckAuthenticationStatus()
        {
            bool isSignedIn = AuthenticationService.Instance.IsSignedIn;
            
            if (isSignedIn && !IsAuthenticated)
            {
                // 서비스는 로그인되어 있지만 매니저 상태가 업데이트되지 않은 경우
                PlayerId = AuthenticationService.Instance.PlayerId;
                PlayerName = AuthenticationService.Instance.PlayerName;
                IsAuthenticated = true;
                
                LogDebug($"인증 상태 복구 - Player ID: {PlayerId}, Name: {PlayerName}");
            }
            else if (!isSignedIn && IsAuthenticated)
            {
                // 서비스는 로그아웃되었지만 매니저 상태가 업데이트되지 않은 경우
                IsAuthenticated = false;
                PlayerId = "";
                PlayerName = "";
                
                LogDebug("인증 상태 초기화");
            }
            
            return IsAuthenticated;
        }
        
        /// <summary>
        /// 플레이어 정보 새로고침
        /// </summary>
        public void RefreshPlayerInfo()
        {
            if (IsAuthenticated)
            {
                PlayerId = AuthenticationService.Instance.PlayerId;
                PlayerName = AuthenticationService.Instance.PlayerName;
                
                LogDebug($"플레이어 정보 새로고침 - ID: {PlayerId}, Name: {PlayerName}");
            }
        }
        
        /// <summary>
        /// 인증 상태 정보 출력
        /// </summary>
        public void LogAuthenticationStatus()
        {
            LogDebug("=== 인증 상태 정보 ===");
            LogDebug($"인증 상태: {IsAuthenticated}");
            LogDebug($"로그인 중: {IsSigningIn}");
            LogDebug($"플레이어 ID: {PlayerId}");
            LogDebug($"플레이어 이름: {PlayerName}");
            LogDebug($"서비스 로그인 상태: {AuthenticationService.Instance.IsSignedIn}");
            LogDebug("=====================");
        }
        
        /// <summary>
        /// 디버그 로그 출력
        /// </summary>
        private void LogDebug(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[AuthenticationManager] {message}");
            }
        }
        
        /// <summary>
        /// 경고 로그 출력
        /// </summary>
        private void LogWarning(string message)
        {
            Debug.LogWarning($"[AuthenticationManager] {message}");
        }
        
        /// <summary>
        /// 에러 로그 출력
        /// </summary>
        private void LogError(string message)
        {
            Debug.LogError($"[AuthenticationManager] {message}");
        }
    }
}
