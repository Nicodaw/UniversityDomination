﻿#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/*
 * This file contains 2 test fixtures, however they are testing the same actual
 * class. The reason for splitting the tests in 2 is to provide extra SetUp
 * methods for each.
 */

public class GameTest_GameInit : BaseGameTest
{
    [SetUp]
    public void GameInit_SetUp() => DefGameInit();

    [Test]
    public void Init_NoSectorHighlighted()
    {
        foreach (Sector sector in map.Sectors)
            Assert.That(sector.Highlighted, Is.False);
    }

    [Test]
    public void Init_CurrentPlayerCorrect()
    {
        // ensure that game loaded player setup data correctly
        Assert.That(Players[0].Kind, Is.EqualTo(PlayerKind.Human));
        Assert.That(Players[1].Kind, Is.EqualTo(PlayerKind.Human));
        Assert.That(Players[2].Kind, Is.EqualTo(PlayerKind.Human));
        Assert.That(Players[3].Kind, Is.EqualTo(PlayerKind.Human));
        Assert.That(Players.Count, Is.EqualTo(4));

        // ensure that the current player is the first one
        Assert.That(game.CurrentPlayer, Is.EqualTo(Players[0]));
    }

    [Test]
    public void Init_Enabled()
    {
        Assert.That(game.enabled, Is.True);
    }

    [Test]
    public void EndTurn_MoveToNextPlayer()
    {
        Assert.That(game.CurrentPlayer, Is.EqualTo(Players[0]));
        game.EndCurrentTurn();
        Assert.That(game.CurrentPlayer, Is.EqualTo(Players[1]));
        game.EndCurrentTurn();
        Assert.That(game.CurrentPlayer, Is.EqualTo(Players[2]));
        game.EndCurrentTurn();
        Assert.That(game.CurrentPlayer, Is.EqualTo(Players[3]));
        game.EndCurrentTurn();
        Assert.That(game.CurrentPlayer, Is.EqualTo(Players[0]));
    }

}

public class GameTest_MapInit : BaseGameTest
{
    [SetUp]
    public void MapInit_SetUp() => DefMapInit();

    Dialog GetGameDialog() => GameObject.Find("Scene").transform.Find("GUI").Find("Dialog").gameObject.GetComponent<Dialog>();

    [Test]
    public void InitMap_MapObjectLoaded()
    {
        Assert.That(map, Is.Not.Null);
    }

    [Test]
    public void InitMap_OneLandmarkAllocatedWithUnitPerPlayer()
    {
        // ensure that each player owns 1 sector and has 1 unit at that sector
        List<Sector> allocatedSectors = new List<Sector>();
        foreach (Player player in Players)
        {
            Assert.That(player.OwnedSectors.Count(), Is.EqualTo(1));
            Assert.That(player.OwnedSectors.First().Landmark, Is.Not.Null);
            Assert.That(player.Units.Count(), Is.EqualTo(1));

            Assert.That(player.OwnedSectors.First(), Is.EqualTo(player.Units.First().Sector));

            allocatedSectors.Add(player.OwnedSectors.First());
        }

        Assert.That(map.Sectors.Count(s => s.Owner != null && !allocatedSectors.Contains(s)),
                    Is.Zero);// must be an error as only sectors owned should be landmarks from above
    }

    [Test]
    public void InitMap_PvcAllocated()
    {
        Assert.That(map.Sectors.Count(s => s.HasPVC), Is.EqualTo(1));
    }

    // ============= event tests? =============

    [Test]
    public void ToggleSaveQuitMenu_Fire()
    {
        Dialog dialog = GetGameDialog();

        Assert.That(dialog.IsShown, Is.False);
        game.ToggleSaveQuitMenu();
        Assert.That(dialog.IsShown, Is.True);
        game.ToggleSaveQuitMenu();
        Assert.That(dialog.IsShown, Is.False);
    }

    [Test]
    public void PlayerEliminated_DialogShow()
    {
        Dialog dialog = GetGameDialog();

        game.PlayerEliminated(Players[0], Players[1]);
        Assert.That(dialog.IsShown, Is.True);
    }

    [UnityTest]
    public IEnumerator PlayerEliminated_DialogOnlyShownOnce()
    {
        Dialog dialog = GetGameDialog();

        // add same player death multiple times
        game.PlayerEliminated(Players[0], Players[1]);
        game.PlayerEliminated(Players[0], Players[2]);
        Assert.That(dialog.IsShown, Is.True);
        dialog.Close();
        // allow couroutine to continue
        yield return null;
        Assert.That(dialog.IsShown, Is.False);
    }

    [Test]
    public void EndGame_DialogShown()
    {
        Dialog dialog = GetGameDialog();

        game.EndGame(game.CurrentPlayer);
        Assert.That(dialog.IsShown, Is.True);
    }

    [Test]
    public void ApplyReward_RewardApplied()
    {
        Game.MinigameReward = new EffectImpl.MinigameRewardEffect(0, 6, 8);
        game.ApplyReward();
        Assert.That(Players[0].Stats.HasEffect<EffectImpl.MinigameRewardEffect>(), Is.True);
        Assert.That(Players[0].Stats.Attack, Is.EqualTo(6));
        Assert.That(Players[0].Stats.Defence, Is.EqualTo(8));

        Game.MinigameReward = new EffectImpl.MinigameRewardEffect(2, 4, 3);
        game.ApplyReward();
        Assert.That(Players[2].Stats.HasEffect<EffectImpl.MinigameRewardEffect>(), Is.True);
        Assert.That(Players[2].Stats.Attack, Is.EqualTo(4));
        Assert.That(Players[2].Stats.Defence, Is.EqualTo(3));
    }

    [Test]
    public void ApplyReward_DialogShown()
    {
        Game.MinigameReward = new EffectImpl.MinigameRewardEffect(0, 1, 1);
        game.ApplyReward();
        Assert.That(GetGameDialog().IsShown, Is.True);
    }

    [Test]
    public void ApplyReward_RewardCleared()
    {
        Game.MinigameReward = new EffectImpl.MinigameRewardEffect(0, 1, 1);
        game.ApplyReward();
        Assert.That(Game.MinigameReward, Is.Null);
    }

    [Test]
    public void ApplyReward_PvcAllocated()
    {
        Sector allocatedSector = map.Sectors.First(s => s.HasPVC);
        Game.MinigameReward = new EffectImpl.MinigameRewardEffect(0, 1, 1);
        game.ApplyReward();
        Assert.That(map.Sectors.First(s => s.HasPVC), Is.Not.EqualTo(allocatedSector));
    }
}
#endif
