using CodeMonkey.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MatchDemo
{
    public class GridGameManager : MonoBehaviour
    {
        [SerializeField] private Demo demoGrid;
        [SerializeField] private Demo demoBar;

        private void Awake()
        {
            demoGrid.OnStepOver += Demo_OnStepOver;
            demoBar.OnDrop += Demo_OnDrop;
        }

        private void Demo_OnStepOver(object sender, Demo.OnStepOverEventArgs e)
        {
            demoBar.SpawnNewCellOnClosestGridPosition(e.cell);
        }

        private void Demo_OnDrop(object sender, Demo.OnDropEventArgs e)
        {
            int dropX;
            int dropY;

            demoGrid.GetGrid().GetXY(e.mouseWorldPosition, out dropX, out dropY);

            if (demoGrid.CanRevealGridPosition(dropX, dropY))
            {
                demoGrid.RevealGridPosition(dropX, dropY, e.savedCellData);
                demoBar.DropSuccess();
            } else
            {

                demoBar.DropFailed();
            }
        }
    }
}
