using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Analytics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Utilities;
using static Oracle;

public class SkrimpInterface : MonoBehaviour
{
    [SerializeField] private Transform portal;
    [SerializeField] private Toggle portalLock;
    [SerializeField] private Color cameraColor;

    [Space(10)] [SerializeField] private Button gravityButton;
    [SerializeField] private Button portalButton;
    [SerializeField] private Button valueButton;
    [SerializeField] private Button countButton;
    [SerializeField] private Button finishLevelButton;
    [SerializeField] private TMP_Text finishLevelText;
    [SerializeField] private SlicedFilledImage progressToNextLevel;
    [SerializeField] private TMP_Text menuIndicatorText;

    [SerializeField] private Image panelExpander;
    [SerializeField] private Color upgradeAvailableColor;

    [Space(10)] [SerializeField] private SlicedFilledImage gravityFillBar;
    [SerializeField] private SlicedFilledImage portalFillBar;
    [SerializeField] private SlicedFilledImage valueFillBar;
    [SerializeField] private SlicedFilledImage countFillBar;
    [Space(10)] [SerializeField] private TMP_Text currencyText;

    [SerializeField] private TMP_Text gravityCost;
    [SerializeField] private TMP_Text gravityAmount;
    [SerializeField] private TMP_Text gravityPerLevel;
    [Space(10)] [SerializeField] private TMP_Text portalCost;
    [SerializeField] private TMP_Text portalAmount;
    [SerializeField] private TMP_Text portalPerLevel;
    [Space(10)] [SerializeField] private TMP_Text valueCost;
    [SerializeField] private TMP_Text valueAmount;
    [SerializeField] private TMP_Text valuePerLevel;
    [Space(10)] [SerializeField] private TMP_Text countCost;
    [SerializeField] private TMP_Text countAmount;
    [SerializeField] private TMP_Text countPerLevel;

    private double nextGravityCost = 9999999999999999;
    private double nextPortalCost = 9999999999999999;
    private double nextValueCost = 9999999999999999;
    private double nextCountCost = 9999999999999999;

    private bool levelComplete => level.currency >= lvlConditions.scoreToAdvance;

    public Level level => oracle.saveData.level;
    private SaveData sd => oracle.saveData;
    public LevelConditions lvlConditions => oracle.levelConditions[oracle.saveData.levelSelector];

    private void Start()
    {
        gravityButton.onClick.AddListener(PurchaseGravity);
        portalButton.onClick.AddListener(PurchasePortal);
        valueButton.onClick.AddListener(PurchaseValue);
        countButton.onClick.AddListener(PurchaseCount);

        finishLevelButton.onClick.AddListener(FinishLevel);

        portalLock.isOn = level.portalLocked;
        portalLock.onValueChanged.AddListener(SetPortalLock);

        Camera.main.backgroundColor = sd.preferences.cameraColorPrefs == CameraColor.Black ? Color.black : cameraColor;

        GetCosts();
    }

    private void SetPortalLock(bool locked)
    {
        level.portalLocked = locked;
    }

    private void FinishLevel()
    {
        oracle.saveData.level.portalLocation = portal.position.x;
        var data = sd.level;
        if (!levelComplete || sd.level.levelComplete || !oracle.levelConditions.ContainsKey(sd.levelSelector + 1))
        {
            sd.savedLevels[sd.levelSelector] = data;
            sd.level = new Level();
            LoadMenu();
            return;
        }

        SendEvent();
        sd.level.levelComplete = true;
        sd.savedLevels[sd.levelSelector] = data;

        sd.levelSelector++;
        if (oracle.levelConditions.ContainsKey(sd.levelSelector))
        {
            sd.level = new Level();
            if (!sd.savedLevels.ContainsKey(sd.levelSelector)) sd.savedLevels.Add(sd.levelSelector, new Level());
        }

        FindObjectOfType<LevelLoader>().LoadScene();
    }

    private void SendEvent()
    {
        var parameters = new Dictionary<string, object>
        {
            { "level", (int)oracle.saveData.levelSelector + 1 },
            { "timeSpentInLevel", CalcUtils.FormatTimeLarge(oracle.saveData.level.levelStats.timeSpentInLevel) }
        };
        AnalyticsService.Instance.CustomData("completedLevel", parameters);
    }


    public void LoadMenu()
    {
        oracle.Save();
        SceneManager.LoadScene(0);
    }

    private void Update()
    {
        oracle.saveData.statistics.timeSpentInLevel += Time.deltaTime;
        oracle.saveData.level.levelStats.timeSpentInLevel += Time.deltaTime;


        var currency = level.currency;
        progressToNextLevel.fillAmount = (float)(currency / lvlConditions.scoreToAdvance);

        finishLevelText.text =
            levelComplete && !sd.level.levelComplete && oracle.levelConditions.ContainsKey(sd.levelSelector + 1)
                ? "Next Level!"
                : "Main Menu";

        var upgradeAvailable = currency >= nextGravityCost || currency >= nextPortalCost || currency >= nextValueCost ||
                               currency >= nextCountCost;
        panelExpander.color = upgradeAvailable ? upgradeAvailableColor : Color.white;
        menuIndicatorText.text = levelComplete
            ? "Level Complete!"
            : $"Goal: {CalcUtils.FormatNumber(lvlConditions.scoreToAdvance)}";


        currencyText.text = CalcUtils.FormatNumber(currency);

        gravityButton.interactable = currency >= nextGravityCost;
        portalButton.interactable = currency >= nextPortalCost && !(level.portalSize >= 4.1);
        valueButton.interactable = currency >= nextValueCost;
        countButton.interactable = currency >= nextCountCost;

        gravityFillBar.fillAmount = (float)(currency / nextGravityCost);
        portalFillBar.fillAmount = level.portalSize >= 4.1 ? 1 : (float)(currency / nextPortalCost);
        valueFillBar.fillAmount = (float)(currency / nextValueCost);
        countFillBar.fillAmount = (float)(currency / nextCountCost);

        gravityCost.text = $"Cost: {CalcUtils.FormatNumber(nextGravityCost)}";
        gravityAmount.text = $"Current Gravity: {level.gravity:F2}";
        gravityPerLevel.text = $"Adds: {CalcUtils.FormatNumber(oracle.data.gravityIncreasePerLevel)}";
        var maxText = "Max";
        portalCost.text = $"Cost: {(level.portalSize >= 4.1 ? maxText : CalcUtils.FormatNumber(nextPortalCost))}";
        portalAmount.text = $"Current Portal Size: {level.portalSize:F2}";
        portalPerLevel.text = $"Adds: {CalcUtils.FormatNumber(oracle.data.portalIncreasePerLevel)}";

        valueCost.text = $"Cost: {CalcUtils.FormatNumber(nextValueCost)}";
        valueAmount.text = $"Current Value: {level.skrimpValue * (1 + oracle.saveData.player.level / 10f):F2}";
        valuePerLevel.text =
            $"Adds: {CalcUtils.FormatNumber(oracle.data.valueIncreasePerLevel * (1 + oracle.saveData.player.level / 10f))}";

        countCost.text = $"Cost: {CalcUtils.FormatNumber(nextCountCost)}";
        countAmount.text =
            $"Current Count: {CalcUtils.FormatNumber(level.skrimpCount + oracle.saveData.player.level / 10)}";
        countPerLevel.text = $"Adds: {CalcUtils.FormatNumber(oracle.data.countIncreasePerLevel)}";
    }

    private void GetCosts()
    {
        nextGravityCost = CalcUtils.BuyX(1, oracle.data.gravityBaseCost,
            oracle.data.gravityCostMulti,
            level.gravityUpgrades);
        nextPortalCost = CalcUtils.BuyX(1, oracle.data.portalBaseCost,
            oracle.data.portalCostMulti,
            level.portalSizeUpgrades);
        nextValueCost = CalcUtils.BuyX(1, oracle.data.valueBaseCost,
            oracle.data.valueCostMulti,
            level.skrimpValueUpgrades);
        nextCountCost = CalcUtils.BuyX(1, oracle.data.countBaseCost,
            oracle.data.countCostMulti,
            level.skrimpCountUpgrades);
    }

    public event Action UpdateSkrimps;

    private void PurchaseGravity()
    {
        level.gravity += oracle.data.gravityIncreasePerLevel;
        level.gravityUpgrades++;
        level.currency -= nextGravityCost;
        GetCosts();
        UpdateSkrimps?.Invoke();
    }

    private void PurchasePortal()
    {
        if (level.portalSize >= 4.1) return;
        level.portalSize += oracle.data.portalIncreasePerLevel;
        level.portalSizeUpgrades++;
        level.currency -= nextPortalCost;
        GetCosts();
        UpdateSkrimps?.Invoke();
    }

    private void PurchaseValue()
    {
        level.skrimpValue += oracle.data.valueIncreasePerLevel;
        level.skrimpValueUpgrades++;
        level.currency -= nextValueCost;
        GetCosts();
        UpdateSkrimps?.Invoke();
    }

    private void PurchaseCount()
    {
        level.skrimpCount += oracle.data.countIncreasePerLevel;
        level.skrimpCountUpgrades++;
        level.currency -= nextCountCost;
        GetCosts();
        UpdateSkrimps?.Invoke();
    }

    public void UpdateSkrimp()
    {
        GetCosts();
        UpdateSkrimps?.Invoke();
    }


    #region Singleton class: Oracle

    public static SkrimpInterface skrimpInterface;


    private void Awake()
    {
        if (skrimpInterface == null)
            skrimpInterface = this;
        else
            Destroy(gameObject);
    }

    #endregion
}