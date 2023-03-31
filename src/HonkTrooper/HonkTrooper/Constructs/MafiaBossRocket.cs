﻿using System;
using System.Linq;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;

namespace HonkTrooper
{
    public partial class MafiaBossRocket : AnimableConstruct
    {
        #region Fields

        private readonly Uri[] _bomb_uris;
        private readonly Uri[] _bomb_blast_uris;

        private readonly Image _content_image;
        private readonly BitmapImage _bitmapImage;

        private double _autoBlastDelay;
        private readonly double _autoBlastDelayDefault = 9;

        private readonly AudioStub _audioStub;

        #endregion

        #region Ctor

        public MafiaBossRocket(
           Func<Construct, bool> animateAction,
           Func<Construct, bool> recycleAction)
        {
            ConstructType = ConstructType.MAFIA_BOSS_ROCKET;

            AnimateAction = animateAction;
            RecycleAction = recycleAction;

            _bomb_uris = Constants.CONSTRUCT_TEMPLATES.Where(x => x.ConstructType == ConstructType.MAFIA_BOSS_ROCKET).Select(x => x.Uri).ToArray();
            _bomb_blast_uris = Constants.CONSTRUCT_TEMPLATES.Where(x => x.ConstructType == ConstructType.BLAST).Select(x => x.Uri).ToArray();
            
            SetConstructSize();

            var uri = ConstructExtensions.GetRandomContentUri(_bomb_uris);
            _bitmapImage = new BitmapImage(uriSource: uri);

            _content_image = new Image()
            {
                Source = _bitmapImage,
                Height = this.Height,
                Width = this.Width,
            };

            SetChild(_content_image);

            BorderThickness = new Microsoft.UI.Xaml.Thickness(Constants.DEFAULT_BLAST_RING_BORDER_THICKNESS);
            CornerRadius = new Microsoft.UI.Xaml.CornerRadius(Constants.DEFAULT_BLAST_RING_CORNER_RADIUS);

            IsometricDisplacement = Constants.DEFAULT_ISOMETRIC_DISPLACEMENT;
            DropShadowDistance = Constants.DEFAULT_DROP_SHADOW_DISTANCE + 10;

            _audioStub = new AudioStub((SoundType.ROCKET_LAUNCH, 0.3, false), (SoundType.ROCKET_BLAST, 1, false));
        }

        #endregion

        #region Methods

        public void Reset()
        {
            _audioStub.Play(SoundType.ROCKET_LAUNCH);

            Opacity = 1;
            SpeedOffset = Constants.DEFAULT_SPEED_OFFSET + 2;

            SetScaleTransform(1);

            BorderBrush = new SolidColorBrush(Colors.Transparent);

            IsBlasting = false;

            var uri = ConstructExtensions.GetRandomContentUri(_bomb_uris);
            _bitmapImage.UriSource = uri;

            AwaitMoveDownLeft = false;
            AwaitMoveUpRight = false;

            AwaitMoveUpLeft = false;
            AwaitMoveDownRight = false;

            _autoBlastDelay = _autoBlastDelayDefault;
        }

        public void Reposition(MafiaBoss mafiaBoss)
        {
            SetPosition(
                left: (mafiaBoss.GetLeft() + mafiaBoss.Width / 2) - Width / 2,
                top: mafiaBoss.GetBottom() - (75));
        }

        public void SetBlast()
        {
            _audioStub.Play(SoundType.ROCKET_BLAST);
            SpeedOffset = Constants.DEFAULT_SPEED_OFFSET - 3;

            SetScaleTransform(Constants.DEFAULT_BLAST_SHRINK_SCALE);
            SetRotation(0);

            BorderBrush = new SolidColorBrush(Colors.Chocolate);

            var uri = ConstructExtensions.GetRandomContentUri(_bomb_blast_uris);
            _bitmapImage.UriSource = uri;

            IsBlasting = true;
        }

        public bool AutoBlast()
        {
            _autoBlastDelay -= 0.1;

            if (_autoBlastDelay <= 0)
                return true;

            return false;
        }

        #endregion
    }
}
