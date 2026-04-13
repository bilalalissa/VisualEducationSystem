#pragma once

#ifdef __cplusplus
extern "C" {
#endif

int VESVision_IsBackendAvailable(void);
const char* VESVision_CopyStatusMessage(void);
const char* VESVision_CopyLatestFrameJson(void);
void VESVision_StopBackend(void);
void VESVision_FreeCopiedString(void* pointer);

#ifdef __cplusplus
}
#endif
