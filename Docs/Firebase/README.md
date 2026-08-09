# 정화 보상 Firestore 등록

이 폴더의 파일은 Stage 1·2 정화 보상 설정을 Firestore에 등록하기 위한 기준 데이터와 시드 스크립트다.

## 파일

- `purify-reward-config.json`: Firestore 문서의 기준 데이터
- `seed-purify-rewards.mjs`: Firebase Admin SDK 등록 스크립트
- `../GameDesign/PurifyRewardBalance.md`: 보상 및 성장 밸런스 기획

## Firestore 경로

```text
/gameConfigs/purifyRewards
/gameConfigs/purifyRewards/stages/stage_001
/gameConfigs/purifyRewards/stages/stage_002
```

`gameConfigs`는 컬렉션, `purifyRewards`는 문서, `stages`는 하위 컬렉션이다.

## 사전 준비

Node.js 실행 환경과 `firebase-admin` 패키지가 필요하다. 별도의 임시 작업 폴더 또는 향후 Cloud Functions 프로젝트에서 다음 명령으로 설치한다.

```powershell
npm install firebase-admin
```

로컬 인증은 Application Default Credentials를 사용한다.

```powershell
gcloud auth application-default login
```

서비스 계정 키를 사용하는 경우 키 파일을 프로젝트 저장소에 커밋하지 않는다.

## 미리보기

스크립트는 기본적으로 Firestore에 접근하지 않고 등록할 문서만 출력한다.

```powershell
node .\Docs\Firebase\seed-purify-rewards.mjs --project=YOUR_FIREBASE_PROJECT_ID
```

출력된 프로젝트 ID, 컬렉션 경로 및 보상 데이터를 검토한다.

## 실제 등록

명시적으로 `--apply`를 전달한 경우에만 Firestore에 기록한다.

```powershell
node .\Docs\Firebase\seed-purify-rewards.mjs --project=YOUR_FIREBASE_PROJECT_ID --apply
```

상위 설정 문서와 두 스테이지 문서는 하나의 Firestore batch로 기록된다. 기존 문서가 있으면 정의된 필드만 병합 갱신한다.

## Firebase 콘솔 수동 등록

Admin SDK를 사용하지 않는 경우 Firebase 콘솔에서 다음 순서로 문서를 생성할 수 있다.

1. `gameConfigs` 컬렉션에 `purifyRewards` 문서를 생성한다.
2. `purifyRewards` 문서 아래에 `stages` 하위 컬렉션을 생성한다.
3. `stage_001`, `stage_002` 문서를 생성한다.
4. `purify-reward-config.json`의 각 `data` 객체를 기준으로 필드를 입력한다.

필드 타입은 다음과 같다.

| 필드 | Firestore 타입 |
|---|---|
| `stageId` | string |
| `enabled` | boolean |
| `configVersion` | number |
| `maxFlow` | number |
| `flowReward` | map |
| `clearRewards` | array of map |
| `firstClearRewards` | array of map |
| `resultRewards` | array of map |
| `randomRewards` | array of map |
| `forestHpBonusEnabled` | boolean |
| `updatedAt` | timestamp |

`resultRewards`의 각 항목은 다음 필드를 사용한다.

| 필드 | Firestore 타입 | 설명 |
|---|---|---|
| `rewardType` | string | 결과에 따라 고정 지급할 보상 타입 |
| `failCompletedFlowAmounts` | array of map | 실패 시 완료 FLOW 기준 고정 수량 구간 |
| `clearAmount` | number | 클리어 시 고정 지급 수량 |

고정 수량 구간은 `minimum`과 `amount`를 가지며 `minimum` 오름차순으로 등록한다. 서버 설정 버전 4에서는 노아의 기억, ElementCore와 가호 소환 티켓을 결과 보상으로 고정 지급한다.

`randomRewards`의 각 항목은 다음 필드를 사용한다.

| 필드 | Firestore 타입 | 설명 |
|---|---|---|
| `rewardType` | string | 이전 설정 스냅샷과 향후 확률 보상에 사용하는 타입 |
| `amount` | number | 당첨 시 지급 수량 |
| `failCompletedFlowChances` | array of map | 실패 시 완료 FLOW 기준 확률 구간 |
| `clearForestHpChances` | array of map | 클리어 시 최종 숲 HP 기준 확률 구간 |

확률 구간은 `minimum`과 `chancePercent`를 가지며 `minimum` 오름차순으로 등록한다. 서버 설정 버전 4의 `randomRewards`는 빈 배열이며, 기존 실행의 설정 스냅샷을 정산하기 위해 서버가 이전 확률 보상 형식을 계속 지원한다.

## 운영 규칙

- 배포 후 기존 보상 조건을 수정할 때는 `configVersion`을 증가시킨다.
- 정화 실행 시작 시 사용한 `configVersion`과 보상 설정 스냅샷을 실행 문서에 기록한다.
- 정산은 최신 설정이 아니라 실행 문서에 저장된 보상 설정 스냅샷을 사용한다.
- 클라이언트는 보상 설정과 지갑을 직접 수정하지 않는다.
- Cloud Functions가 보상을 계산하고 지갑, 진행도, 정산 기록을 트랜잭션으로 갱신한다.
- `rewardType` 문자열은 Unity와 Cloud Functions에서 동일한 상수로 관리한다.
- 기존 사용자 지갑의 `blessingTicket` 필드는 `initializeWallet` 호출 시 0으로 초기화한다.
- 고정 결과 보상을 지원하는 Functions를 배포한 뒤 Firestore 보상 설정을 v4로 갱신한다. 기존 v2·v3 실행은 저장된 설정 스냅샷으로 정산한다.
