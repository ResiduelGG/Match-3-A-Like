using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MatchDemo
{
    public class GridGameManager : MonoBehaviour
    {
        [SerializeField] private Demo grid;
        [SerializeField] private Demo bar;

        private void Awake()
        {
            grid.OnStepOver += Demo_OnStepOver;
        }

        private void Demo_OnStepOver(object sender, Demo.OnStepOverEventArgs e)
        {
            bar.SpawnNewCellOnClosestGridPosition(e.cell);
            //bar.TryFindMatchesAndDestroyThem();
        }
    }
}
