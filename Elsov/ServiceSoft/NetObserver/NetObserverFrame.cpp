// NetObserverFrame.cpp : implementation of the CNetObserverFrame class
//

#include "stdafx.h"
#include "NetObserver.h"

#include "NetObserverFrame.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

/////////////////////////////////////////////////////////////////////////////
// CNetObserverFrame

IMPLEMENT_DYNAMIC(CNetObserverFrame, CMDIFrameWnd)

BEGIN_MESSAGE_MAP(CNetObserverFrame, CMDIFrameWnd)
	//{{AFX_MSG_MAP(CNetObserverFrame)
	ON_WM_CREATE()
	ON_WM_QUERYENDSESSION()
	//}}AFX_MSG_MAP
END_MESSAGE_MAP()

static UINT indicators[] =
{
	ID_SEPARATOR,           // status line indicator
	ID_INDICATOR_CAPS,
	ID_INDICATOR_NUM,
	ID_INDICATOR_SCRL,
};

/////////////////////////////////////////////////////////////////////////////
// CNetObserverFrame construction/destruction

CNetObserverFrame::CNetObserverFrame()
{
	// TODO: add member initialization code here
	
}

CNetObserverFrame::~CNetObserverFrame()
{
}

int CNetObserverFrame::OnCreate(LPCREATESTRUCT lpCreateStruct)
{
	if (CMDIFrameWnd::OnCreate(lpCreateStruct) == -1)
		return -1;
	
	if (!m_wndToolBar.CreateEx(this, TBSTYLE_FLAT, WS_CHILD | WS_VISIBLE | CBRS_TOP
		| CBRS_GRIPPER | CBRS_TOOLTIPS | CBRS_FLYBY | CBRS_SIZE_DYNAMIC) ||
		!m_wndToolBar.LoadToolBar(IDR_MAINFRAME))
	{
		TRACE0("Failed to create toolbar\n");
		return -1;      // fail to create
	}

	if (!m_wndStatusBar.Create(this) ||
		!m_wndStatusBar.SetIndicators(indicators,
		  sizeof(indicators)/sizeof(UINT)))
	{
		TRACE0("Failed to create status bar\n");
		return -1;      // fail to create
	}

	// TODO: Delete these three lines if you don't want the toolbar to
	//  be dockable
	m_wndToolBar.EnableDocking(CBRS_ALIGN_ANY);
	EnableDocking(CBRS_ALIGN_ANY);
	DockControlBar(&m_wndToolBar);

	return 0;
}

BOOL CNetObserverFrame::PreCreateWindow(CREATESTRUCT &cs)
{
	if( !CMDIFrameWnd::PreCreateWindow(cs) )
		return FALSE;
	// TODO: Modify the Window class or styles here by modifying
	//  the CREATESTRUCT cs

	return TRUE;
}

/////////////////////////////////////////////////////////////////////////////
// CNetObserverFrame diagnostics

#ifdef _DEBUG
void CNetObserverFrame::AssertValid() const
{
	CMDIFrameWnd::AssertValid();
}

void CNetObserverFrame::Dump(CDumpContext &dc) const
{
	CMDIFrameWnd::Dump(dc);
}

#endif //_DEBUG

/////////////////////////////////////////////////////////////////////////////
// CNetObserverFrame message handlers


BOOL CNetObserverFrame::OnQueryEndSession() 
{
	TRACE("CNetObserverFrame::OnQueryEndSession\n");
	if (!CMDIFrameWnd::OnQueryEndSession())
		return FALSE;
	
	GetApplication()->KillAllThreads();
	
	return TRUE;
}
