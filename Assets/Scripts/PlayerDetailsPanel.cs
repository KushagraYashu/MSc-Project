using NUnit.Framework;
using System.Linq;
using TMPro;
using UnityEngine;

public class PlayerDetailsPanel : MonoBehaviour
{
    public TMP_Text[] historyTexts;
    public TMP_Text ratingText;
    public TMP_Text KDRText;
    public TMP_Text gamesText;

    public void ShowDetails(Player p)
    {
        switch (MainServer.instance.SystemIndex)
        {
            case 0: //elo
                GetComponent<GraphMaker>().ShowGraph(p.EloHistory);
                ratingText.text = p.playerData.Elo.ToString("F6");
                break;

            case 1: //glicko
                GetComponent<GraphMaker>().ShowGraph(p.EloHistory);
                ratingText.text = p.playerData.Elo.ToString("F6");
                break;

            case 2: //vanilla trueskill (moserware)
                GetComponent<GraphMaker>().ShowGraph(p.scaledRatingHistory);
                ratingText.text = VanillaTrueskillSystemManager.instance.ConvertRating((float)p.playerData.TrueSkillRating.Mean, CentralProperties.instance.eloRangePerPool[0].x, CentralProperties.instance.eloRangePerPool[CentralProperties.instance.totPools - 1].y, VanillaTrueskillSystemManager.RatingConversion.To_MyRating).ToString("F6");
                break;

            case 3: //smart match
                GetComponent<GraphMaker>().ShowGraph(p.EloHistory);
                ratingText.text = p.playerData.CompositeSkill.ToString("F6");
                break;
        }

        KDRText.text = p.playerData.KDR.ToString("F2");
        gamesText.text = $"{p.playerData.GamesPlayed} Games";

        var outcomes = p.playerData.Outcomes;
        foreach(var text in historyTexts)
        {
            text.text = ""; // Clear previous texts
            text.color = Color.white; // Reset color
        }
        for (int i = outcomes.Count - 1, j = 0; j < historyTexts.Length; i--, j++)
        {
            if (i < 0) break; // Prevent index out of range

            if (outcomes[i] == 1)
            {
                historyTexts[j].text = "W";
                historyTexts[j].color = Color.green;
            }
            else
            {
                historyTexts[j].text = "L";
                historyTexts[j].color = Color.red;
            }
        }

        
    }
}
