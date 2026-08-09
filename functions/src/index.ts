import {setGlobalOptions} from "firebase-functions/v2";

setGlobalOptions({
  region: "asia-northeast3",
  maxInstances: 10,
  timeoutSeconds: 30,
});

export {startPurify} from "./purify/startPurify";
export {settlePurify} from "./purify/settlePurify";
export {upgradeCollection} from "./collection/upgradeCollection";
export {unlockNoa} from "./noa/unlockNoa";
export {summonBlessing} from "./blessing/summonBlessing";
export {initializeWallet} from "./wallet/initializeWallet";
export {ensureUserProfile} from "./user/ensureUserProfile";
export {updateUserName} from "./user/updateUserName";
export {withdrawAccount} from "./user/withdrawAccount";
