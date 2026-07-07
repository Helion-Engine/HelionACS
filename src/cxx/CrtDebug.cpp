#ifdef _CRTDEBUGENABLE
#include <windows.h>
#include <crtdbg.h>
#endif
#include "CrtDebug.hpp"

static bool Init = false;

void CrtDebugInit()
{
#ifdef _CRTDEBUGENABLE
    if (Init)
        return;

    Init = true;
    int flags = _CrtSetDbgFlag(_CRTDBG_REPORT_FLAG);
    flags |= _CRTDBG_ALLOC_MEM_DF | _CRTDBG_DELAY_FREE_MEM_DF | _CRTDBG_CHECK_ALWAYS_DF | _CRTDBG_LEAK_CHECK_DF;
    _CrtSetDbgFlag(flags);
#endif
}
