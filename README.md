# Poker (WIP)

1:1 텍사스 홀덤 멀티플레이 포커 프로젝트입니다.  
Unity Netcode for GameObjects와 Unity Services(Anonymous Auth, Relay)를 사용합니다. 현재 네트워크/로비 기능을 먼저 구현 중입니다.

> 상태: WIP(작업 진행 중)

## 주요 기능
- 로비: 방 만들기, 코드로 참여, 퀵 매치, 방 목록, 종료
- Unity Relay로 호스트 방식 노출 없이 연결
- Netcode 씬 동기화(서버가 씬 전환, 클라이언트 자동 동기화)

## 기술 스택
- Unity 6 + URP, Netcode for GameObjects
- Unity Services: Authentication(Anonymous), Relay

## 프로젝트 구조
```
Assets/Scripts/
  Core/           # Singleton 등 코어 유틸
  Network/        # UGS 초기화/인증/Relay/세션 제어
  UI/Lobby/       # 로비 매니저/뷰
  Game/           # (WIP) 포커 로직/동기화
```

## 사용 에셋
- CardEase — Maziminds