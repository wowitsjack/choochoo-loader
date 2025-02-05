/*
    choochoo.c

    ChooChoo is a Proton trainer/trainer loader that took TOO LONG fnasdkljgbfasdljgb

    Compile with:
         gcc choochoo.c -mwindows -o choochoo.exe -lcomctl32 -lshlwapi
*/

#define UNICODE
#define _WIN32_WINNT 0x0A00
#define _WIN32_IE    0x0A00

#include <windows.h>
#include <commctrl.h>
#include <shlwapi.h>
#include <wchar.h>
#include <stdio.h>
#include <stdlib.h>

#pragma comment(lib, "comctl32.lib")
#pragma comment(lib, "shlwapi.lib")

//-----------------------------------------------------------------
// Define missing ComboBox helper macros if not already defined

#ifndef ComboBox_AddString
#define ComboBox_AddString(hwnd, lpsz) ((int)SendMessageW((hwnd), CB_ADDSTRING, 0, (LPARAM)(lpsz)))
#endif

#ifndef ComboBox_GetCount
#define ComboBox_GetCount(hwnd) ((int)SendMessageW((hwnd), CB_GETCOUNT, 0, 0))
#endif

#ifndef ComboBox_GetLBText
#define ComboBox_GetLBText(hwnd, index, buffer) ((int)SendMessageW((hwnd), CB_GETLBTEXT, (WPARAM)(index), (LPARAM)(buffer)))
#endif

#ifndef ComboBox_FindStringExact
#define ComboBox_FindStringExact(hwnd, start, lpsz) ((int)SendMessageW((hwnd), CB_FINDSTRINGEXACT, (WPARAM)(start), (LPARAM)(lpsz)))
#endif

#ifndef ComboBox_InsertString
#define ComboBox_InsertString(hwnd, index, lpsz) ((int)SendMessageW((hwnd), CB_INSERTSTRING, (WPARAM)(index), (LPARAM)(lpsz)))
#endif

#ifndef ComboBox_DeleteString
#define ComboBox_DeleteString(hwnd, index) ((int)SendMessageW((hwnd), CB_DELETESTRING, (WPARAM)(index), 0))
#endif

#ifndef ComboBox_ResetContent
#define ComboBox_ResetContent(hwnd) ((int)SendMessageW((hwnd), CB_RESETCONTENT, 0, 0))
#endif

//-----------------------------------------------------------------
// Macros and Constant Definitions

// Recents key for the INI file.
#define RECENT_KEY L"RecentPaths"

// Control IDs
#define IDC_GROUP_PATHS           600
#define IDC_GROUP_PROFILES        300

#define IDC_STATIC_STATUS         101

// Game Path
#define IDC_STATIC_GAME           102
#define IDC_COMBO_GAME            120
#define IDC_BUTTON_BROWSE_GAME    112

// Trainer Path
#define IDC_STATIC_TRAINER        103
#define IDC_COMBO_TRAINER         121
#define IDC_BUTTON_BROWSE_TRAINER 114

// Additional EXEs/DLLs (fixed 5 rows)
#define MAX_ADDITIONAL            5
#define IDC_CHECK_EXE1            200
#define IDC_CHECK_EXE2            201
#define IDC_CHECK_EXE3            202
#define IDC_CHECK_EXE4            203
#define IDC_CHECK_EXE5            204

#define IDC_COMBO_EXE1            230
#define IDC_COMBO_EXE2            231
#define IDC_COMBO_EXE3            232
#define IDC_COMBO_EXE4            233
#define IDC_COMBO_EXE5            234

#define IDC_BUTTON_BROWSE_EXE1    220
#define IDC_BUTTON_BROWSE_EXE2    221
#define IDC_BUTTON_BROWSE_EXE3    222
#define IDC_BUTTON_BROWSE_EXE4    223
#define IDC_BUTTON_BROWSE_EXE5    224

// Auto Launcher checkbox
#define IDC_CHECK_AUTO_LAUNCH     520

// Profiles area (editable combo box plus buttons)
#define IDC_PROFILES_COMBO        400
#define IDC_BUTTON_REFRESH_PROFILES 401
#define IDC_BUTTON_LOAD_PROFILE   402
#define IDC_BUTTON_SAVE_PROFILE   403

// Launch button
#define IDC_BUTTON_LAUNCH         104

// Status log (read-only multi-line Edit)
#define IDC_STATUS_LOG            500

//-----------------------------------------------------------------
// Global Variables

HINSTANCE hInst = NULL;
HANDLE hJob = NULL;
int g_additionalCount = MAX_ADDITIONAL;  // Fixed to 5 additional rows

//-----------------------------------------------------------------
// Forward Declarations of Helper Functions

BOOL FileExistsW(const wchar_t *path);
BOOL LaunchProcessW(const wchar_t *appName, wchar_t *cmdLine, const wchar_t *workDir, BOOL newConsole, DWORD *pPID, HANDLE *phProcess);
void GetDirectory(LPCWSTR path, LPWSTR dir, int dirSize);
BOOL BrowseFile(HWND owner, LPWSTR buffer, DWORD bufferSize);
void TrimNewline(WCHAR *str);
void ExtractExecutableFromCmdLine(LPCWSTR cmdLine, LPWSTR exePath, int exePathSize);

void LoadRecentsForCombo(HWND hCombo);
void UpdateRecentsForCombo(HWND hCombo);
void AddRecent(HWND hCombo, LPCWSTR text);

void EnsureProfilesDir(void);
void SaveProfileToFile(LPCWSTR filename, HWND hwnd);
void LoadProfileFromFile(LPCWSTR filename, HWND hwnd);
void UpdateProfileList(HWND hCombo);
void AppendLog(HWND hLog, LPCWSTR msg);

BOOL GetAutoLaunchSetting(void);
void SaveAutoLaunchSetting(BOOL enabled);
LRESULT CALLBACK AutoLaunchProc(HWND hWnd, UINT message, WPARAM wParam, LPARAM lParam);
void DoAutoLaunchWindow(HWND hParent);

LRESULT CALLBACK WndEraseBkGnd(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam);
LRESULT CALLBACK WndProc(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam);

//-----------------------------------------------------------------
// New: Group Box Subclass Procedure to fix background painting under Wine/Proton

LRESULT CALLBACK GroupBoxSubclassProc(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam)
{
    if (msg == WM_ERASEBKGND)
    {
        HDC hdc = (HDC)wParam;
        RECT rc;
        GetClientRect(hwnd, &rc);
        FillRect(hdc, &rc, GetSysColorBrush(COLOR_BTNFACE));
        return 1;
    }
    // Retrieve the original window procedure stored as a property.
    WNDPROC oldProc = (WNDPROC)GetPropW(hwnd, L"OldGroupBoxProc");
    return CallWindowProcW(oldProc, hwnd, msg, wParam, lParam);
}

//-----------------------------------------------------------------
// Function Definitions

BOOL FileExistsW(const wchar_t *path)
{
    DWORD attrib = GetFileAttributesW(path);
    return (attrib != INVALID_FILE_ATTRIBUTES && !(attrib & FILE_ATTRIBUTE_DIRECTORY));
}

BOOL LaunchProcessW(const wchar_t *appName, wchar_t *cmdLine, const wchar_t *workDir, BOOL newConsole, DWORD *pPID, HANDLE *phProcess)
{
    const wchar_t *fileToLaunch = (appName && wcslen(appName) > 0) ? appName : cmdLine;
    const wchar_t *pExt = PathFindExtensionW(fileToLaunch);
    if (pExt && (lstrcmpiW(pExt, L".bat") == 0 ||
                 lstrcmpiW(pExt, L".cmd") == 0 ||
                 lstrcmpiW(pExt, L".com") == 0))
    {
        static wchar_t newCmd[1024];
        wsprintfW(newCmd, L"cmd.exe /C \"%s\"", fileToLaunch);
        appName = NULL;
        cmdLine = newCmd;
    }
    STARTUPINFOW si;
    PROCESS_INFORMATION pi;
    DWORD flags = 0;
    ZeroMemory(&si, sizeof(si));
    si.cb = sizeof(si);
    ZeroMemory(&pi, sizeof(pi));
    if (newConsole)
        flags |= CREATE_NEW_CONSOLE | CREATE_NEW_PROCESS_GROUP;
    if (!CreateProcessW(appName, cmdLine, NULL, NULL, FALSE, flags, NULL, workDir, &si, &pi))
    {
        wchar_t msg[256];
        wsprintfW(msg, L"Failed to launch process:\n%s", cmdLine);
        MessageBoxW(NULL, msg, L"Launch Error", MB_ICONERROR);
        return FALSE;
    }
    if (pPID)
        *pPID = pi.dwProcessId;
    if (phProcess)
        *phProcess = pi.hProcess;
    else
        CloseHandle(pi.hProcess);
    CloseHandle(pi.hThread);
    return TRUE;
}

void GetDirectory(LPCWSTR path, LPWSTR dir, int dirSize)
{
    wcsncpy(dir, path, dirSize);
    dir[dirSize - 1] = L'\0';
    LPWSTR p = wcsrchr(dir, L'\\');
    if (p)
        *p = L'\0';
}

BOOL BrowseFile(HWND owner, LPWSTR buffer, DWORD bufferSize)
{
    OPENFILENAMEW ofn;
    ZeroMemory(&ofn, sizeof(ofn));
    buffer[0] = L'\0';
    ofn.lStructSize = sizeof(ofn);
    ofn.hwndOwner = owner;
    ofn.lpstrFile = buffer;
    ofn.nMaxFile = bufferSize;
    // Allow DLLs as well.
    ofn.lpstrFilter = L"Executable/Batch Files (*.exe;*.bat;*.cmd;*.com;*.dll)\0*.exe;*.bat;*.cmd;*.com;*.dll\0All Files (*.*)\0*.*\0";
    ofn.Flags = OFN_PATHMUSTEXIST | OFN_FILEMUSTEXIST;
    return GetOpenFileNameW(&ofn);
}

void TrimNewline(WCHAR *str)
{
    int len = (int)wcslen(str);
    while (len > 0 && (str[len - 1] == L'\n' || str[len - 1] == L'\r'))
    {
        str[len - 1] = L'\0';
        len--;
    }
}

void ExtractExecutableFromCmdLine(LPCWSTR cmdLine, LPWSTR exePath, int exePathSize)
{
    if (cmdLine[0] == L'\"')
    {
        const wchar_t *p = wcschr(cmdLine + 1, L'\"');
        if (p)
        {
            int len = (int)(p - cmdLine - 1);
            if (len > exePathSize - 1)
                len = exePathSize - 1;
            wcsncpy(exePath, cmdLine + 1, len);
            exePath[len] = L'\0';
        }
        else
        {
            wcsncpy(exePath, cmdLine, exePathSize);
            exePath[exePathSize - 1] = L'\0';
        }
    }
    else
    {
        int i = 0;
        while (cmdLine[i] != L'\0' && cmdLine[i] != L' ')
        {
            if (i < exePathSize - 1)
                exePath[i] = cmdLine[i];
            i++;
        }
        exePath[i] = L'\0';
    }
}

void LoadRecentsForCombo(HWND hCombo)
{
    WCHAR buffer[1024] = L"";
    GetPrivateProfileStringW(L"Recents", RECENT_KEY, L"", buffer, 1024, L"recent.ini");
    wchar_t *token = wcstok(buffer, L";");
    while (token)
    {
        TrimNewline(token);
        if (wcslen(token) > 0)
            ComboBox_AddString(hCombo, token);
        token = wcstok(NULL, L";");
    }
}

void UpdateRecentsForCombo(HWND hCombo)
{
    int count = ComboBox_GetCount(hCombo);
    WCHAR all[1024] = L"";
    for (int i = 0; i < count; i++)
    {
        WCHAR item[256];
        ComboBox_GetLBText(hCombo, i, item);
        if (wcslen(item) > 0)
        {
            if (i > 0)
                wcscat(all, L";");
            wcscat(all, item);
        }
    }
    WritePrivateProfileStringW(L"Recents", RECENT_KEY, all, L"recent.ini");
}

void AddRecent(HWND hCombo, LPCWSTR text)
{
    if (wcslen(text) == 0)
        return;
    int index = ComboBox_FindStringExact(hCombo, -1, text);
    if (index == CB_ERR)
    {
        ComboBox_InsertString(hCombo, 0, text);
        while (ComboBox_GetCount(hCombo) > 10)
            ComboBox_DeleteString(hCombo, ComboBox_GetCount(hCombo) - 1);
        UpdateRecentsForCombo(hCombo);
    }
}

void EnsureProfilesDir(void)
{
    CreateDirectoryW(L"profiles", NULL);
}

void SaveProfileToFile(LPCWSTR filename, HWND hwnd)
{
    // Save Game, Trainer, and Additional EXE/DLL settings.
    WCHAR gamePath[1024] = L"", trainerPath[1024] = L"", exePath[1024] = L"";
    GetWindowTextW(GetDlgItem(hwnd, IDC_COMBO_GAME), gamePath, 1024);
    GetWindowTextW(GetDlgItem(hwnd, IDC_COMBO_TRAINER), trainerPath, 1024);
    FILE *f = _wfopen(filename, L"w");
    if (f)
    {
        fwprintf(f, L"%s\r\n", gamePath);
        fwprintf(f, L"%s\r\n", trainerPath);
        for (int i = 0; i < g_additionalCount; i++)
        {
            int checkID = IDC_CHECK_EXE1 + i;
            int comboID = IDC_COMBO_EXE1 + i;
            BOOL flag = (IsDlgButtonChecked(hwnd, checkID) == BST_CHECKED);
            GetWindowTextW(GetDlgItem(hwnd, comboID), exePath, 1024);
            fwprintf(f, L"%d,%s\r\n", flag ? 1 : 0, exePath);
        }
        fclose(f);
    }
    else
    {
        MessageBoxW(hwnd, L"Failed to save profile.", L"Error", MB_ICONERROR);
    }
}

void LoadProfileFromFile(LPCWSTR filename, HWND hwnd)
{
    FILE *f = _wfopen(filename, L"r");
    if (f)
    {
        WCHAR gamePath[1024] = L"", trainerPath[1024] = L"", line[1024];
        if (fgetws(gamePath, 1024, f) != NULL) TrimNewline(gamePath);
        if (fgetws(trainerPath, 1024, f) != NULL) TrimNewline(trainerPath);
        SetWindowTextW(GetDlgItem(hwnd, IDC_COMBO_GAME), gamePath);
        SetWindowTextW(GetDlgItem(hwnd, IDC_COMBO_TRAINER), trainerPath);
        for (int i = 0; i < g_additionalCount; i++)
        {
            if (fgetws(line, 1024, f) != NULL)
            {
                TrimNewline(line);
                int flag = 0;
                WCHAR path[1024] = L"";
                swscanf(line, L"%d,%[^\r\n]", &flag, path);
                if (flag)
                    CheckDlgButton(hwnd, IDC_CHECK_EXE1 + i, BST_CHECKED);
                else
                    CheckDlgButton(hwnd, IDC_CHECK_EXE1 + i, BST_UNCHECKED);
                SetWindowTextW(GetDlgItem(hwnd, IDC_COMBO_EXE1 + i), path);
            }
        }
        fclose(f);
    }
    else
    {
        MessageBoxW(hwnd, L"Failed to load profile.", L"Error", MB_ICONERROR);
    }
}

void UpdateProfileList(HWND hCombo)
{
    EnsureProfilesDir();
    ComboBox_ResetContent(hCombo);
    WIN32_FIND_DATAW fd;
    HANDLE hFind = FindFirstFileW(L"profiles\\*.ini", &fd);
    if (hFind != INVALID_HANDLE_VALUE)
    {
        do
        {
            if (lstrcmpiW(fd.cFileName, L"last.ini") == 0)
                continue;
            WCHAR name[256];
            wcscpy(name, fd.cFileName);
            PathRemoveExtensionW(name);
            ComboBox_AddString(hCombo, name);
        } while (FindNextFileW(hFind, &fd));
        FindClose(hFind);
    }
}

void AppendLog(HWND hLog, LPCWSTR msg)
{
    int len = GetWindowTextLengthW(hLog);
    SendMessageW(hLog, EM_SETSEL, (WPARAM)len, (LPARAM)len);
    SendMessageW(hLog, EM_REPLACESEL, 0, (LPARAM)msg);
    SendMessageW(hLog, EM_REPLACESEL, 0, (LPARAM)L"\r\n");
}

BOOL GetAutoLaunchSetting(void)
{
    WCHAR buf[16];
    GetPrivateProfileStringW(L"AutoLaunch", L"Enabled", L"0", buf, 16, L"autolaunch.ini");
    return (buf[0] == L'1');
}

void SaveAutoLaunchSetting(BOOL enabled)
{
    WritePrivateProfileStringW(L"AutoLaunch", L"Enabled", enabled ? L"1" : L"0", L"autolaunch.ini");
}

LRESULT CALLBACK AutoLaunchProc(HWND hWnd, UINT message, WPARAM wParam, LPARAM lParam)
{
    static int countdown = 5;
    switch (message)
    {
        case WM_CREATE:
            countdown = 5;
            SetTimer(hWnd, 1, 1000, NULL);
            return 0;
        case WM_TIMER:
        {
            countdown--;
            WCHAR buf[128];
            wsprintfW(buf, L"AUTOLAUNCHING IN (%ds)...", countdown);
            SetWindowTextW(hWnd, buf);
            if (countdown <= 0)
            {
                KillTimer(hWnd, 1);
                HWND hParent = GetParent(hWnd);
                if (hParent)
                    SendMessageW(hParent, WM_COMMAND, IDC_BUTTON_LAUNCH, 0);
                DestroyWindow(hWnd);
            }
            return 0;
        }
        case WM_COMMAND:
            if (LOWORD(wParam) == IDCANCEL)
            {
                KillTimer(hWnd, 1);
                SaveAutoLaunchSetting(FALSE);
                DestroyWindow(hWnd);
            }
            return 0;
        default:
            return DefWindowProcW(hWnd, message, wParam, lParam);
    }
}

void DoAutoLaunchWindow(HWND hParent)
{
    WNDCLASSW wc = {0};
    wc.lpfnWndProc = AutoLaunchProc;
    wc.hInstance = hInst;
    wc.lpszClassName = L"AutoLaunchWindowClass";
    wc.hbrBackground = (HBRUSH)(COLOR_BTNFACE+1);
    RegisterClassW(&wc);

    RECT rcParent;
    GetWindowRect(hParent, &rcParent);
    int width = 300, height = 150;
    int x = rcParent.left + ((rcParent.right - rcParent.left) - width) / 2;
    int y = rcParent.top + ((rcParent.bottom - rcParent.top) - height) / 2;

    HWND hDlg = CreateWindowExW(WS_EX_TOPMOST, L"AutoLaunchWindowClass", L"AUTOLAUNCHING IN (5s)...",
            WS_POPUP | WS_BORDER | WS_CAPTION,
            x, y, width, height,
            hParent, NULL, hInst, NULL);
    CreateWindowExW(0, L"BUTTON", L"Cancel", WS_CHILD | WS_VISIBLE | BS_DEFPUSHBUTTON,
                     100, 80, 100, 30, hDlg, (HMENU)IDCANCEL, hInst, NULL);
    ShowWindow(hDlg, SW_SHOW);
    UpdateWindow(hDlg);
    
    MSG msg;
    while (GetMessageW(&msg, NULL, 0, 0))
    {
        if (!IsDialogMessageW(hDlg, &msg))
        {
            TranslateMessage(&msg);
            DispatchMessageW(&msg);
        }
        if (!IsWindow(hDlg))
            break;
    }
}

// Custom WM_ERASEBKGND handler to fill background with the proper system color.
LRESULT CALLBACK WndEraseBkGnd(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam)
{
    if (msg == WM_ERASEBKGND)
    {
        HDC hdc = (HDC)wParam;
        RECT rc;
        GetClientRect(hwnd, &rc);
        FillRect(hdc, &rc, GetSysColorBrush(COLOR_BTNFACE));
        return 1;
    }
    return DefWindowProcW(hwnd, msg, wParam, lParam);
}

//-----------------------------------------------------------------
// Main Window Procedure

LRESULT CALLBACK WndProc(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam)
{
    static HWND hGroupPaths, hGroupProfiles;
    static HWND hStaticGame, hComboGame, hButtonBrowseGame;
    static HWND hStaticTrainer, hComboTrainer, hButtonBrowseTrainer;
    static HWND hStaticAddExes;
    // Fixed 5 additional EXE/DLL rows as direct children.
    static HWND hCheckExe[5], hComboExe[5], hButtonBrowseExe[5];
    static HWND hProfilesCombo;
    static HWND hButtonRefreshProfiles, hButtonLoadProfile, hButtonSaveProfile;
    static HWND hStatusLog;
    static HWND hButtonLaunch;
    static HWND hCheckAutoLaunch;
    HFONT hFont = (HFONT)GetStockObject(DEFAULT_GUI_FONT);

    // --- WM_CTLCOLOR handling for standard controls.
    switch (msg)
    {
        case WM_CTLCOLORBTN:
        {
            HDC hdcButton = (HDC)wParam;
            SetBkMode(hdcButton, TRANSPARENT);
            return (LRESULT)GetSysColorBrush(COLOR_BTNFACE);
        }
        case WM_CTLCOLORSTATIC:
        {
            HDC hdcStatic = (HDC)wParam;
            SetBkColor(hdcStatic, GetSysColor(COLOR_BTNFACE));
            return (LRESULT)GetSysColorBrush(COLOR_BTNFACE);
        }
    }

    if (msg == WM_ERASEBKGND)
        return WndEraseBkGnd(hwnd, msg, wParam, lParam);

    switch (msg)
    {
        case WM_CREATE:
        {
            // Create "Paths" group box.
            hGroupPaths = CreateWindowExW(0, L"BUTTON", L"Paths",
                             WS_CHILD | WS_VISIBLE | BS_GROUPBOX,
                             10, 10, 540, 280,
                             hwnd, (HMENU)IDC_GROUP_PATHS, hInst, NULL);
            SendMessageW(hGroupPaths, WM_SETFONT, (WPARAM)hFont, TRUE);
            // Subclass the group box to fix its background on Wine.
            {
                WNDPROC oldProc = (WNDPROC)SetWindowLongPtrW(hGroupPaths, GWLP_WNDPROC, (LONG_PTR)GroupBoxSubclassProc);
                SetPropW(hGroupPaths, L"OldGroupBoxProc", (HANDLE)oldProc);
            }

            // Game Path.
            hStaticGame = CreateWindowExW(0, L"STATIC", L"Game Path:",
                             WS_CHILD | WS_VISIBLE,
                             20, 30, 100, 20,
                             hwnd, (HMENU)IDC_STATIC_GAME, hInst, NULL);
            SendMessageW(hStaticGame, WM_SETFONT, (WPARAM)hFont, TRUE);
            hComboGame = CreateWindowExW(WS_EX_CLIENTEDGE, L"COMBOBOX", L"",
                             WS_CHILD | WS_VISIBLE | CBS_DROPDOWN | CBS_AUTOHSCROLL,
                             130, 30, 300, 200,
                             hwnd, (HMENU)IDC_COMBO_GAME, hInst, NULL);
            SendMessageW(hComboGame, WM_SETFONT, (WPARAM)hFont, TRUE);
            hButtonBrowseGame = CreateWindowExW(0, L"BUTTON", L"Browse...",
                             WS_CHILD | WS_VISIBLE,
                             450, 30, 80, 20,
                             hwnd, (HMENU)IDC_BUTTON_BROWSE_GAME, hInst, NULL);
            SendMessageW(hButtonBrowseGame, WM_SETFONT, (WPARAM)hFont, TRUE);

            // Trainer Path.
            hStaticTrainer = CreateWindowExW(0, L"STATIC", L"Trainer Path:",
                             WS_CHILD | WS_VISIBLE,
                             20, 60, 100, 20,
                             hwnd, (HMENU)IDC_STATIC_TRAINER, hInst, NULL);
            SendMessageW(hStaticTrainer, WM_SETFONT, (WPARAM)hFont, TRUE);
            hComboTrainer = CreateWindowExW(WS_EX_CLIENTEDGE, L"COMBOBOX", L"",
                             WS_CHILD | WS_VISIBLE | CBS_DROPDOWN | CBS_AUTOHSCROLL,
                             130, 60, 300, 200,
                             hwnd, (HMENU)IDC_COMBO_TRAINER, hInst, NULL);
            SendMessageW(hComboTrainer, WM_SETFONT, (WPARAM)hFont, TRUE);
            hButtonBrowseTrainer = CreateWindowExW(0, L"BUTTON", L"Browse...",
                             WS_CHILD | WS_VISIBLE,
                             450, 60, 80, 20,
                             hwnd, (HMENU)IDC_BUTTON_BROWSE_TRAINER, hInst, NULL);
            SendMessageW(hButtonBrowseTrainer, WM_SETFONT, (WPARAM)hFont, TRUE);

            // Label for Additional EXEs/DLLs.
            hStaticAddExes = CreateWindowExW(0, L"STATIC", L"Additional EXEs/DLLs:",
                             WS_CHILD | WS_VISIBLE,
                             20, 90, 180, 20,
                             hwnd, NULL, hInst, NULL);
            SendMessageW(hStaticAddExes, WM_SETFONT, (WPARAM)hFont, TRUE);

            // Create fixed Additional EXE/DLL rows (5 rows) starting at y = 120.
            for (int i = 0; i < MAX_ADDITIONAL; i++)
            {
                int y = 120 + i * 30;
                hCheckExe[i] = CreateWindowExW(0, L"BUTTON", L"Launch",
                    WS_CHILD | WS_VISIBLE | BS_AUTOCHECKBOX,
                    20, y, 60, 20,
                    hwnd, (HMENU)(IDC_CHECK_EXE1 + i), hInst, NULL);
                SendMessageW(hCheckExe[i], WM_SETFONT, (WPARAM)hFont, TRUE);

                hComboExe[i] = CreateWindowExW(WS_EX_CLIENTEDGE, L"COMBOBOX", L"",
                    WS_CHILD | WS_VISIBLE | CBS_DROPDOWN | CBS_AUTOHSCROLL,
                    90, y, 300, 200,
                    hwnd, (HMENU)(IDC_COMBO_EXE1 + i), hInst, NULL);
                SendMessageW(hComboExe[i], WM_SETFONT, (WPARAM)hFont, TRUE);

                hButtonBrowseExe[i] = CreateWindowExW(0, L"BUTTON", L"Browse...",
                    WS_CHILD | WS_VISIBLE,
                    400, y, 80, 20,
                    hwnd, (HMENU)(IDC_BUTTON_BROWSE_EXE1 + i), hInst, NULL);
                SendMessageW(hButtonBrowseExe[i], WM_SETFONT, (WPARAM)hFont, TRUE);
            }

            // Auto Launcher checkbox.
            hCheckAutoLaunch = CreateWindowExW(0, L"BUTTON", L"Auto Launcher",
                             WS_CHILD | WS_VISIBLE | BS_AUTOCHECKBOX,
                             20, 460, 200, 25,
                             hwnd, (HMENU)IDC_CHECK_AUTO_LAUNCH, hInst, NULL);
            SendMessageW(hCheckAutoLaunch, WM_SETFONT, (WPARAM)hFont, TRUE);

            // Profiles Group Box.
            hGroupProfiles = CreateWindowExW(0, L"BUTTON", L"Profiles",
                             WS_CHILD | WS_VISIBLE | BS_GROUPBOX,
                             560, 10, 220, 280,
                             hwnd, (HMENU)IDC_GROUP_PROFILES, hInst, NULL);
            SendMessageW(hGroupProfiles, WM_SETFONT, (WPARAM)hFont, TRUE);
            // Subclass the profiles group box as well.
            {
                WNDPROC oldProc = (WNDPROC)SetWindowLongPtrW(hGroupProfiles, GWLP_WNDPROC, (LONG_PTR)GroupBoxSubclassProc);
                SetPropW(hGroupProfiles, L"OldGroupBoxProc", (HANDLE)oldProc);
            }
            hProfilesCombo = CreateWindowExW(WS_EX_CLIENTEDGE, L"COMBOBOX", L"",
                             WS_CHILD | WS_VISIBLE | CBS_DROPDOWN | CBS_AUTOHSCROLL,
                             570, 40, 180, 200,
                             hwnd, (HMENU)IDC_PROFILES_COMBO, hInst, NULL);
            SendMessageW(hProfilesCombo, WM_SETFONT, (WPARAM)hFont, TRUE);
            {
                HWND hButtonRefreshProfiles = CreateWindowExW(0, L"BUTTON", L"Refresh",
                             WS_CHILD | WS_VISIBLE,
                             570, 75, 80, 25,
                             hwnd, (HMENU)IDC_BUTTON_REFRESH_PROFILES, hInst, NULL);
                SendMessageW(hButtonRefreshProfiles, WM_SETFONT, (WPARAM)hFont, TRUE);
                HWND hButtonLoadProfile = CreateWindowExW(0, L"BUTTON", L"Load",
                             WS_CHILD | WS_VISIBLE,
                             670, 75, 80, 25,
                             hwnd, (HMENU)IDC_BUTTON_LOAD_PROFILE, hInst, NULL);
                SendMessageW(hButtonLoadProfile, WM_SETFONT, (WPARAM)hFont, TRUE);
                HWND hButtonSaveProfile = CreateWindowExW(0, L"BUTTON", L"Save",
                             WS_CHILD | WS_VISIBLE,
                             570, 110, 180, 25,
                             hwnd, (HMENU)IDC_BUTTON_SAVE_PROFILE, hInst, NULL);
                SendMessageW(hButtonSaveProfile, WM_SETFONT, (WPARAM)hFont, TRUE);
            }
            UpdateProfileList(hProfilesCombo);

            // Status Log.
            hStatusLog = CreateWindowExW(WS_EX_CLIENTEDGE, L"EDIT", L"",
                             WS_CHILD | WS_VISIBLE | ES_MULTILINE | ES_READONLY | WS_VSCROLL,
                             10, 300, 540, 150,
                             hwnd, (HMENU)IDC_STATUS_LOG, hInst, NULL);
            SendMessageW(hStatusLog, WM_SETFONT, (WPARAM)hFont, TRUE);

            // Launch Button.
            hButtonLaunch = CreateWindowExW(0, L"BUTTON", L"Launch",
                             WS_CHILD | WS_VISIBLE | BS_DEFPUSHBUTTON,
                             220, 510, 560, 40,
                             hwnd, (HMENU)IDC_BUTTON_LAUNCH, hInst, NULL);
            SendMessageW(hButtonLaunch, WM_SETFONT, (WPARAM)hFont, TRUE);

            // Load recents.
            LoadRecentsForCombo(hComboGame);
            LoadRecentsForCombo(hComboTrainer);
            for (int i = 0; i < g_additionalCount; i++)
                LoadRecentsForCombo(GetDlgItem(hwnd, IDC_COMBO_EXE1 + i));

            // If Auto Launcher is enabled, load last settings from "profiles\\last.ini"
            // and post the auto-launch message.
            if (GetAutoLaunchSetting())
            {
                WCHAR lastFile[MAX_PATH];
                wsprintfW(lastFile, L"profiles\\last.ini");
                if (FileExistsW(lastFile))
                    LoadProfileFromFile(lastFile, hwnd);
                CheckDlgButton(hwnd, IDC_CHECK_AUTO_LAUNCH, BST_CHECKED);
                PostMessage(hwnd, WM_USER+100, 0, 0);
            }
        }
        break;

        case WM_USER+100:
            DoAutoLaunchWindow(hwnd);
            break;

        case WM_COMMAND:
            switch (LOWORD(wParam))
            {
                case IDC_CHECK_AUTO_LAUNCH:
                {
                    BOOL checked = (IsDlgButtonChecked(hwnd, IDC_CHECK_AUTO_LAUNCH) == BST_CHECKED);
                    SaveAutoLaunchSetting(checked);
                }
                break;
                case IDC_BUTTON_BROWSE_GAME:
                {
                    WCHAR fileBuf[MAX_PATH] = L"";
                    if (BrowseFile(hwnd, fileBuf, MAX_PATH))
                        SetWindowTextW(GetDlgItem(hwnd, IDC_COMBO_GAME), fileBuf);
                }
                break;
                case IDC_BUTTON_BROWSE_TRAINER:
                {
                    WCHAR fileBuf[MAX_PATH] = L"";
                    if (BrowseFile(hwnd, fileBuf, MAX_PATH))
                        SetWindowTextW(GetDlgItem(hwnd, IDC_COMBO_TRAINER), fileBuf);
                }
                break;
                default:
                    if (LOWORD(wParam) >= IDC_BUTTON_BROWSE_EXE1 && LOWORD(wParam) < IDC_BUTTON_BROWSE_EXE1 + g_additionalCount)
                    {
                        int id = LOWORD(wParam) - IDC_BUTTON_BROWSE_EXE1;
                        WCHAR fileBuf[MAX_PATH] = L"";
                        if (BrowseFile(hwnd, fileBuf, MAX_PATH))
                            SetWindowTextW(GetDlgItem(hwnd, IDC_COMBO_EXE1 + id), fileBuf);
                        break;
                    }
                    else if (LOWORD(wParam) == IDC_BUTTON_REFRESH_PROFILES)
                    {
                        UpdateProfileList(GetDlgItem(hwnd, IDC_PROFILES_COMBO));
                        break;
                    }
                    else if (LOWORD(wParam) == IDC_BUTTON_LOAD_PROFILE)
                    {
                        WCHAR name[MAX_PATH];
                        GetWindowTextW(GetDlgItem(hwnd, IDC_PROFILES_COMBO), name, MAX_PATH);
                        if (wcslen(name) == 0)
                        {
                            MessageBoxW(hwnd, L"Please enter a profile name to load.", L"Error", MB_ICONERROR);
                            break;
                        }
                        WCHAR filePath[MAX_PATH];
                        wsprintfW(filePath, L"profiles\\%s.ini", name);
                        if (!FileExistsW(filePath))
                        {
                            MessageBoxW(hwnd, L"Profile not found.", L"Error", MB_ICONERROR);
                            break;
                        }
                        LoadProfileFromFile(filePath, hwnd);
                        break;
                    }
                    else if (LOWORD(wParam) == IDC_BUTTON_SAVE_PROFILE)
                    {
                        EnsureProfilesDir();
                        WCHAR name[MAX_PATH];
                        GetWindowTextW(GetDlgItem(hwnd, IDC_PROFILES_COMBO), name, MAX_PATH);
                        if (wcslen(name) == 0)
                        {
                            MessageBoxW(hwnd, L"Please enter a profile name to save.", L"Error", MB_ICONERROR);
                            break;
                        }
                        WCHAR filePath[MAX_PATH];
                        wsprintfW(filePath, L"profiles\\%s.ini", name);
                        SaveProfileToFile(filePath, hwnd);
                        UpdateProfileList(GetDlgItem(hwnd, IDC_PROFILES_COMBO));
                        break;
                    }
                    else if (LOWORD(wParam) == IDC_BUTTON_LAUNCH)
                    {
                        // Save current settings to "profiles\\last.ini" for autolaunch restoration.
                        {
                            WCHAR lastFile[MAX_PATH];
                            wsprintfW(lastFile, L"profiles\\last.ini");
                            SaveProfileToFile(lastFile, hwnd);
                        }
                        SetWindowTextW(GetDlgItem(hwnd, IDC_STATUS_LOG), L"");
                        WCHAR gameCmd[1024] = L"", trainerCmd[1024] = L"", exeCmd[1024] = L"";
                        WCHAR exePathExtracted[1024] = L"", dir[MAX_PATH] = L"";
                        GetWindowTextW(GetDlgItem(hwnd, IDC_COMBO_GAME), gameCmd, 1024);
                        GetWindowTextW(GetDlgItem(hwnd, IDC_COMBO_TRAINER), trainerCmd, 1024);
                        // Launch Game.
                        if (wcslen(gameCmd) > 0)
                        {
                            if (!FileExistsW(gameCmd))
                                AppendLog(GetDlgItem(hwnd, IDC_STATUS_LOG), L"Game: FAILED (not found)");
                            else {
                                ExtractExecutableFromCmdLine(gameCmd, exePathExtracted, 1024);
                                GetDirectory(exePathExtracted, dir, MAX_PATH);
                                if (LaunchProcessW(NULL, gameCmd, dir, TRUE, NULL, NULL))
                                {
                                    AppendLog(GetDlgItem(hwnd, IDC_STATUS_LOG), L"Game: LAUNCHED");
                                    AddRecent(GetDlgItem(hwnd, IDC_COMBO_GAME), gameCmd);
                                }
                                else
                                    AppendLog(GetDlgItem(hwnd, IDC_STATUS_LOG), L"Game: FAILED");
                            }
                        }
                        // Launch Trainer.
                        if (wcslen(trainerCmd) > 0)
                        {
                            if (!FileExistsW(trainerCmd))
                                AppendLog(GetDlgItem(hwnd, IDC_STATUS_LOG), L"Trainer: FAILED (not found)");
                            else {
                                ExtractExecutableFromCmdLine(trainerCmd, exePathExtracted, 1024);
                                GetDirectory(exePathExtracted, dir, MAX_PATH);
                                if (LaunchProcessW(NULL, trainerCmd, dir, TRUE, NULL, NULL))
                                {
                                    AppendLog(GetDlgItem(hwnd, IDC_STATUS_LOG), L"Trainer: LAUNCHED");
                                    AddRecent(GetDlgItem(hwnd, IDC_COMBO_TRAINER), trainerCmd);
                                }
                                else
                                    AppendLog(GetDlgItem(hwnd, IDC_STATUS_LOG), L"Trainer: FAILED");
                            }
                        }
                        // Launch Additional EXEs/DLLs.
                        for (int i = 0; i < g_additionalCount; i++)
                        {
                            if (IsDlgButtonChecked(hwnd, IDC_CHECK_EXE1 + i) == BST_CHECKED)
                            {
                                GetWindowTextW(GetDlgItem(hwnd, IDC_COMBO_EXE1 + i), exeCmd, 1024);
                                if (wcslen(exeCmd) > 0)
                                {
                                    if (!FileExistsW(exeCmd))
                                    {
                                        wchar_t errMsg[256];
                                        wsprintfW(errMsg, L"Additional EXE/DLL %d: FAILED (not found): %s", i+1, exeCmd);
                                        AppendLog(GetDlgItem(hwnd, IDC_STATUS_LOG), errMsg);
                                    }
                                    else {
                                        const wchar_t *ext = PathFindExtensionW(exeCmd);
                                        if (ext && _wcsicmp(ext, L".dll") == 0)
                                        {
                                            HMODULE hMod = LoadLibraryW(exeCmd);
                                            if (hMod)
                                            {
                                                wchar_t msg[256];
                                                wsprintfW(msg, L"Additional DLL %d: LOADED", i+1);
                                                AppendLog(GetDlgItem(hwnd, IDC_STATUS_LOG), msg);
                                                AddRecent(GetDlgItem(hwnd, IDC_COMBO_EXE1 + i), exeCmd);
                                            }
                                            else
                                            {
                                                wchar_t msg[256];
                                                wsprintfW(msg, L"Additional DLL %d: FAILED to load", i+1);
                                                AppendLog(GetDlgItem(hwnd, IDC_STATUS_LOG), msg);
                                            }
                                        }
                                        else
                                        {
                                            ExtractExecutableFromCmdLine(exeCmd, exePathExtracted, 1024);
                                            GetDirectory(exePathExtracted, dir, MAX_PATH);
                                            if (LaunchProcessW(NULL, exeCmd, dir, TRUE, NULL, NULL))
                                            {
                                                wchar_t msg[256];
                                                wsprintfW(msg, L"Additional EXE %d: LAUNCHED", i+1);
                                                AppendLog(GetDlgItem(hwnd, IDC_STATUS_LOG), msg);
                                                AddRecent(GetDlgItem(hwnd, IDC_COMBO_EXE1 + i), exeCmd);
                                            }
                                            else
                                            {
                                                wchar_t msg[256];
                                                wsprintfW(msg, L"Additional EXE %d: FAILED", i+1);
                                                AppendLog(GetDlgItem(hwnd, IDC_STATUS_LOG), msg);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    }
            }
            break;

        case WM_DESTROY:
            PostQuitMessage(0);
            break;
        
        default:
            return DefWindowProcW(hwnd, msg, wParam, lParam);
    }
    return 0;
}

//-----------------------------------------------------------------
// Main Entry Point

int APIENTRY wWinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance,
                      LPWSTR lpCmdLine, int nCmdShow)
{
    WCHAR exePath[MAX_PATH];
    WCHAR *lastSlash;
    hInst = hInstance;
    GetModuleFileNameW(NULL, exePath, MAX_PATH);
    lastSlash = wcsrchr(exePath, L'\\');
    if (lastSlash)
        *lastSlash = L'\0';
    SetCurrentDirectoryW(exePath);

    // Create a Job Object so that all launched processes are terminated on exit.
    HANDLE hJob = CreateJobObjectW(NULL, NULL);
    if (hJob)
    {
        JOBOBJECT_EXTENDED_LIMIT_INFORMATION jeli;
        ZeroMemory(&jeli, sizeof(jeli));
        jeli.BasicLimitInformation.LimitFlags = JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE;
        SetInformationJobObject(hJob, JobObjectExtendedLimitInformation, &jeli, sizeof(jeli));
    }

    INITCOMMONCONTROLSEX icex;
    icex.dwSize = sizeof(icex);
    icex.dwICC  = ICC_STANDARD_CLASSES;
    InitCommonControlsEx(&icex);

    WNDCLASSW wc = {0};
    wc.lpfnWndProc   = WndProc;
    wc.hInstance     = hInstance;
    wc.lpszClassName = L"ChooChooWindowClass";
    wc.hCursor       = LoadCursorW(NULL, IDC_ARROW);
    wc.hbrBackground = GetSysColorBrush(COLOR_BTNFACE);
    RegisterClassW(&wc);

    // Create main window without WS_EX_COMPOSITED.
    HWND hwnd = CreateWindowExW(0, L"ChooChooWindowClass", L"ChooChoo - Proton/WINE Trainer/DLL Loader",
                      WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_MINIMIZEBOX | WS_CLIPCHILDREN,
                      CW_USEDEFAULT, CW_USEDEFAULT, 800, 600,
                      NULL, NULL, hInstance, NULL);
    if (!hwnd)
        return -1;
    ShowWindow(hwnd, nCmdShow);
    UpdateWindow(hwnd);
    MSG msg;
    while (GetMessageW(&msg, NULL, 0, 0))
    {
        TranslateMessage(&msg);
        DispatchMessageW(&msg);
    }
    if (hJob)
        CloseHandle(hJob);
    return (int)msg.wParam;
}

#ifdef UNICODE
int WINAPI WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance,
                   LPSTR lpCmdLine, int nCmdShow)
{
    LPWSTR lpCmdLineW = GetCommandLineW();
    return wWinMain(hInstance, hPrevInstance, lpCmdLineW, nCmdShow);
}
#endif
