using UnityEngine;
using UnityEngine.UI;

public class PlayerShowBox : MonoBehaviour
{
    public GameObject IDTxtGO;
    public GameObject eloTxtGO;
    public Image bgImage;

    public PlayerShowBox nextBox;

    public Player associatedPlayer;

    public PlayerDetailsPanel detailsPanel;

    public void LateUpdate()
    {
        if (this.gameObject.activeInHierarchy)
        {
            if (this.associatedPlayer != null && this.associatedPlayer.representationDirty)
            {
                Player p = this.associatedPlayer;

                switch (MainServer.instance.SystemIndex)
                {
                    case 0: //elo
                        this.eloTxtGO.GetComponent<TMPro.TMP_Text>().text = p.playerData.Elo.ToString("F4");
                        break;

                    case 1: //glicko
                        this.eloTxtGO.GetComponent<TMPro.TMP_Text>().text = p.playerData.Elo.ToString("F4");
                        break;

                    case 2: //vanilla trueskill (moserware)
                        this.eloTxtGO.GetComponent<TMPro.TMP_Text>().text = p.playerData.TrueSkillScaled(CentralProperties.instance.eloRangePerPool[0].x, CentralProperties.instance.eloRangePerPool[CentralProperties.instance.totPools - 1].y).ToString("F4");
                        break;

                    case 3: //smart match
                        this.eloTxtGO.GetComponent<TMPro.TMP_Text>().text = p.playerData.CompositeSkill.ToString("F4");
                        break;
                }
            }

            if (this.detailsPanel != null && this.detailsPanel.gameObject.activeInHierarchy && associatedPlayer.representationDirty)
            {
                this.detailsPanel.ShowDetails(associatedPlayer);
            }
        }

    }
}
