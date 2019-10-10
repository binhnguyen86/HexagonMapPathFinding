using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : Singleton<UIManager>
{
    public GameObject PauseBtn;
    public GameObject ResumeBtn;
    public Text SpeedUpText;
    public Slider DefenderPower;
    public Slider AttackerPower;
    public Image DefenderHpImg;
    public Image AttackerHpImg;
    public Color DefenderHp;
    public Color DefenderHpDefault;
    public Color AttackerHp;
    public Color AttackerHpDefault;
    public CanvasGroup PowerContainer;
    public CanvasGroup SettingPanel;
    public CanvasGroup ResetPanel;

    private int _currentSpeed;
    private bool _attackerDamaged;
    private bool _defenderDamaged;

    private void Start()
    {
        Time.timeScale = 1;
        ResetPanel.alpha = 0;
        ResetPanel.blocksRaycasts = false;
        SettingPanel.alpha = 1;
        SettingPanel.blocksRaycasts = true;
        PauseBtn.SetActive(true);
        ResumeBtn.SetActive(false);
        _currentSpeed = 1;
        SpeedUpText.text = string.Format("x{0}", _currentSpeed);
        PowerContainer.alpha = 0;
    }

    public void Pause(bool pause)
    {
        PauseBtn.SetActive(!pause);
        ResumeBtn.SetActive(pause);
        Time.timeScale = pause ? 0 : _currentSpeed;
    }

    public void SpeedUp(int value)
    {
        _currentSpeed = Mathf.Max(1, _currentSpeed + value);
        ApplySpeed();
    }

    private void ApplySpeed()
    {
        Time.timeScale = _currentSpeed;
        SpeedUpText.text = string.Format("x{0}", _currentSpeed);
    }

    public void SetupPower(int attackerCount, int defenderCount)
    {
        int attackerPower = attackerCount * 10;
        int defenderPower = defenderCount * 30;
        int totalPower = Mathf.Max(attackerPower, defenderPower);
        DefenderPower.maxValue = totalPower;
        AttackerPower.maxValue = totalPower;
        DefenderPower.value = defenderPower;
        AttackerPower.value = attackerPower;
        PowerContainer.alpha = 1;
    }

    private void AttackerHitted(int damage)
    {
        AttackerPower.value -= damage;
        _attackerDamaged = true;
    }

    private void DefenderHitted(int damage)
    {
        DefenderPower.value -= damage;
        _defenderDamaged = true;
    }

    private void Update()
    {
        if ( _defenderDamaged )
        {
            DefenderHp = Color.white;
        }
        else
        {
            if ( DefenderHp == DefenderHpDefault )
            {
                // return if there is no color change
                return;
            }
            DefenderHp = Color.Lerp(DefenderHp, DefenderHpDefault, 3 * Time.deltaTime);
        }
        DefenderHpImg.color = DefenderHp;
        _defenderDamaged = false;

        if ( _attackerDamaged )
        {
            AttackerHp = Color.white;
        }
        else
        {
            if ( AttackerHp == AttackerHpDefault )
            {
                // return if there is no color change
                return;
            }
            AttackerHp = Color.Lerp(AttackerHp, AttackerHpDefault, 3 * Time.deltaTime);
        }
        AttackerHpImg.color = AttackerHp;
        _attackerDamaged = false;
    }

    public void SetupGamePlay(int mapSize)
    {
        HexagonMapGenerator.Instance.SetupGamePlay(mapSize);
        SettingPanel.alpha = 0;
        SettingPanel.blocksRaycasts = false;
    }

    public void RestartGame()
    {
        SceneManager.LoadScene("Main", LoadSceneMode.Single);
    }

    public void OpenRestartPanel()
    {
        ResetPanel.alpha = 1;
        ResetPanel.blocksRaycasts = true;
    }
}
