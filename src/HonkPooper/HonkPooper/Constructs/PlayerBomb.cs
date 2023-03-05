﻿using System;
using System.Linq;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Controls;

namespace HonkPooper
{
    public partial class PlayerBomb : Construct
    {
        #region Fields

        private Random _random;
        private Uri[] _bomb_uris;
        private Uri[] _bomb_blast_uris;

        private int _blastDelay;
        private readonly int _blastDelayDefault = 15;

        #endregion

        #region Properties

        public bool IsBlasting { get; set; }

        #endregion

        #region Ctor

        public PlayerBomb(
            Func<Construct, bool> animateAction,
            Func<Construct, bool> recycleAction,
            double downScaling)
        {
            _random = new Random();

            _bomb_uris = Constants.CONSTRUCT_TEMPLATES.Where(x => x.ConstructType == ConstructType.PLAYER_BOMB).Select(x => x.Uri).ToArray();
            _bomb_blast_uris = Constants.CONSTRUCT_TEMPLATES.Where(x => x.ConstructType == ConstructType.PLAYER_BOMB_BLAST).Select(x => x.Uri).ToArray();

            var size = Constants.CONSTRUCT_SIZES.FirstOrDefault(x => x.ConstructType == ConstructType.PLAYER_BOMB);

            ConstructType = ConstructType.PLAYER_BOMB;

            var width = size.Width * downScaling;
            var height = size.Height * downScaling;

            AnimateAction = animateAction;
            RecycleAction = recycleAction;

            SetSize(width: width, height: height);

            var uri = _bomb_uris[_random.Next(0, _bomb_uris.Length)];

            var content = new Image()
            {
                Source = new BitmapImage(uriSource: uri)
            };

            SetChild(content);

            IsometricDisplacement = 0.5;
            SpeedOffset = Constants.DEFAULT_SPEED_OFFSET;
            DropShadowDistance = 40;

            _blastDelay = _blastDelayDefault;
        }

        #endregion

        #region Methods

        public void Reposition(Player player, double downScaling)
        {
            SetPosition(
                left: (player.GetLeft() + player.Width / 2) - Width / 2,
                top: player.GetBottom() - (40 * downScaling),
                z: 7);
        }

        public void Reset()
        {
            _blastDelay = _blastDelayDefault;

            Opacity = 1;
            SetScaleTransform(1);
            IsBlasting = false;

            var uri = _bomb_uris[_random.Next(0, _bomb_uris.Length)];

            var content = new Image()
            {
                Source = new BitmapImage(uriSource: uri)
            };

            SetChild(content);
        }

        public void SetBlastContent()
        {
            var uri = _bomb_blast_uris[_random.Next(0, _bomb_blast_uris.Length)];

            var content = new Image()
            {
                Source = new BitmapImage(uriSource: uri)
            };

            SetChild(content);

            IsBlasting = true;
        }

        #endregion
    }
}