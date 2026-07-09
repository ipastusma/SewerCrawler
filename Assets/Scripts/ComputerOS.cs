using UnityEngine;
// UI 요소를 제어하기 위해 반드시 필요한 네임스페이스입니다.
using UnityEngine.UI; 

public class ComputerOS : MonoBehaviour
{
    [Header("UI 패널 설정")]
    // 바탕화면 위에 뜰 기사창과 이메일창 오브젝트를 담는 변수.
    // 인스펙터에서 직접 드래그 앤 드롭으로 연결.
    public GameObject newsPanel;  
    public GameObject emailPanel; 

    // 게임이 시작될 때 단 한 번 실행.
    void Start()
    {
        // 컴퓨터를 처음 켰을 때 창이 다 열려 있으면 안 되므로 초기화.
        CloseAllWindows();
    }

    // [OpenNews] 인터넷 아이콘 버튼에 연결할 함수.
    // public이 붙어야 유니티 버튼의 OnClick 이벤트에서 이 함수를 찾을 수 있음.
    public void OpenNews()
    {
        CloseAllWindows();      // 다른 창이 열려 있다면 먼저 다 닫는다.
        newsPanel.SetActive(true); // 뉴스 패널만 활성화한다.
        Debug.Log("뉴스 기사를 불러왔습니다.");
    }

    // [OpenEmail] 이메일 아이콘 버튼에 연결할 함수.
    public void OpenEmail()
    {
        CloseAllWindows();
        emailPanel.SetActive(true); // 이메일 패널만 활성화.
        Debug.Log("이메일 시스템에 접속했습니다.");
    }

    // 모든 창을 끄는 로직.
    // 중복 코드를 줄이기 위해 별도의 함수로 생성.
    public void CloseAllWindows()
    {
        // .SetActive(false)는 오브젝트를 Hide 처리하고 연산을 중지.
        if(newsPanel != null) newsPanel.SetActive(false);
        if(emailPanel != null) emailPanel.SetActive(false);
    }
}