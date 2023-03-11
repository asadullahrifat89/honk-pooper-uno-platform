﻿using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using Windows.UI;
using static System.Formats.Asn1.AsnWriter;

namespace HonkTrooper
{
    public partial class ScoreBar : Border
    {
        private int BossPointScore { get; set; } = 0;

        private int Score { get; set; } = 0;

        private TextBlock TextBlock { get; set; } = new TextBlock() { FontSize = 30, FontWeight = FontWeights.Bold };

        public ScoreBar()
        {
            VerticalAlignment = VerticalAlignment.Center;
            HorizontalAlignment = HorizontalAlignment.Center;

            this.Child = TextBlock;
            GainScore(0);
        }

        public void Reset() 
        {
            Score = 0;
            TextBlock.Text = Score.ToString("0000");
        }

        public void GainScore(int score)
        {
            Score += score;
            TextBlock.Text = Score.ToString("0000");
        }

        public int GetScore()
        {
            return Score;
        }

        public bool IsBossPointScore(int scoreDiff)
        {
            var bossPoint = Score - BossPointScore > scoreDiff;

            if (bossPoint)
                BossPointScore = Score;

            return bossPoint;
        }
    }
}
