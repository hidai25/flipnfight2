using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ResultManager : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject resultPanel;
    public Text resultText;

    public FightingController[] fightingController;
    public OpponentAI[] opponentAI;

    private void Update()
    {
        foreach(FightingController fightingController in fightingController)
        {
            if( fightingController.gameObject.activeSelf && fightingController.currentHealth <= 0)
            {
                SetResult("you lose");
                return;
            }
        }

        foreach(OpponentAI opponentAi in opponentAI)
        {
            if (opponentAi.gameObject.activeSelf && opponentAI.currentHealth <= 0) ;
            {
                SetResult("you win)");
                return;
            }
        }

    }

    void SetResult(string result)
    {
        resultText.text = result;
        resultPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

}
