# 수확 주식회사 (Harvest Inc)

**낫 한 자루로 시작해 농기계 군단을 거느리는 농장 방치형 게임**

밭을 가득 채운 작물을 베고, 번 돈으로 예초기와 콤바인을 들이고, 명성을 쌓아 더 큰 밭으로 옮겨 가세요. 농장이 충분히 커지면 과감히 팔고 새 땅을 개척해 더 큰 수확에 도전합니다.

- **손맛 있는 수확**: 버튼을 연타하거나 작물을 직접 눌러 베어 냅니다.
- **알아서 일하는 농기계**: 예초기는 자주 3칸을, 콤바인은 한 줄 전체를 쓸어 갑니다.
- **골라 가는 밭**: 밀, 벼, 옥수수, 당근, 호박. 명성으로 새 밭을 열고 언제든 오갈 수 있습니다.
- **새 농장 개척**: 농장을 팔고 황금 씨앗을 받아, 사라지지 않는 영구 강화를 삽니다.

## 다운로드

| 플랫폼 | 파일 |
|---|---|
| PC (Windows) | [HarvestInc_Setup.exe](https://github.com/InheritO/Harvest-Inc/releases/latest/download/HarvestInc_Setup.exe) |
| Android | [HarvestInc.apk](https://github.com/InheritO/Harvest-Inc/releases/latest/download/HarvestInc.apk) |

이전 버전은 [Releases](https://github.com/InheritO/Harvest-Inc/releases)에서 받을 수 있습니다.

## 설치 방법

### PC (Windows)

1. `HarvestInc_Setup.exe`를 받아 실행합니다.
2. "Windows의 PC 보호" 창이 뜨면 **추가 정보 → 실행**을 누릅니다. 개인이 만든 게임이라 서명이 없어서 나오는 안내입니다.
3. 설치 마법사에서 **다음**을 눌러 진행합니다. 바탕화면 바로가기가 필요하면 체크하세요.
4. 시작 메뉴나 바탕화면의 **수확 주식회사**를 실행합니다.

| 항목 | 경로 |
|---|---|
| 게임 | `%LOCALAPPDATA%\Programs\수확 주식회사` |
| 저장 데이터 | `%USERPROFILE%\AppData\LocalLow\InheritO\Harvest Inc\save.json` |

- 관리자 권한으로 설치했다면 게임은 `C:\Program Files\수확 주식회사`에 설치됩니다.
- **제거**: Windows 설정 → 앱 → 설치된 앱 → 수확 주식회사 → 제거. 저장 데이터는 남아서 다시 설치하면 이어서 할 수 있습니다.
- **업데이트**: 새 버전 설치 파일을 실행하면 기존 설치 위에 덮어씁니다. 저장 데이터는 그대로입니다.
- 창모드(세로 화면)로 실행됩니다. 실행 중 **Alt + Enter**로 전체 화면과 바꿀 수 있습니다.

### Android

1. 휴대폰에서 `HarvestInc.apk`를 받습니다.
2. 받은 파일을 누르면 **"출처를 알 수 없는 앱"** 설치를 허용할지 묻습니다. **설정**에서 파일을 연 앱(브라우저, 파일 관리자 등)에 **이 출처 허용**을 켭니다.
3. **설치**를 누릅니다. "Play 프로텍트" 경고가 나오면 **세부정보 → 무시하고 설치**를 누릅니다.
4. 홈 화면의 **수확 주식회사**를 실행합니다.

- **업데이트**: 새 apk를 설치하면 기존 앱 위에 업데이트됩니다.
- **제거**: 앱 아이콘을 길게 누르고 → 제거. 저장 데이터도 함께 지워집니다.

## 플레이 방법

1. **수확**: 아래 수확 버튼을 누르세요. 꾹 누르면 연달아 벱니다. 작물을 직접 눌러도 됩니다.
2. **강화**: 상점에서 낫을 강화하고 농기계를 사세요. 왼쪽 아래 원이 다 차면 농기계가 작동합니다.
3. **밭 이동**: 밭을 모두 비우면 명성(★)을 얻습니다. 위쪽 밭 이름 버튼을 눌러 다음 밭을 여세요.
4. **개척**: 옥수수 밭을 열면 상점에 개척 탭이 생깁니다. 명성을 모아 새 농장을 개척하세요.

| 모바일 | PC | 동작 |
|---|---|---|
| 수확 버튼 | Space | 낫질 (누르고 있으면 연속) |
| 작물 누르기 | 작물 클릭 | 그 작물을 노려 낫질 |
| 설정 버튼 | ESC | 설정 (소리, 진동, 저장, 게임 방법, 종료) |

진행은 자동으로 저장됩니다.

## 문서

- [게임 소개](https://github.com/InheritO/Harvest-Inc/blob/main/Docs/introduction.md)
- [기획서 v1.0](https://github.com/InheritO/Harvest-Inc/blob/main/Docs/design-v1.0.md)

## 개발

- 엔진: Unity 6 (6000.3.28f1), uGUI, Input System
- `Assets/Scenes/MainScene`을 열고 Play를 누르면 화면 전체가 코드로 만들어집니다.
- 밸런스 수치는 모두 `Assets/Scripts/Balance.cs`에 있습니다.

## 크레딧

- 배경음악: "Fun on the Farm" — geoffharvey (Pixabay)
- 그 밖의 그림과 효과음: 코드로 직접 제작
