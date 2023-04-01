﻿using Microsoft.UI.Xaml.Controls;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Linq;

namespace HonkTrooper
{
    public partial class UfoBossRocketSeeking : RocketSeeking
    {
        #region Fields

        private readonly Uri[] _bomb_uris;
        private readonly Uri[] _bomb_blast_uris;

        private readonly Image _content_image;
        private readonly BitmapImage _bitmapImage;

        private double _autoBlastDelay;
        private readonly double _autoBlastDelayDefault = 25;

        private readonly AudioStub _audioStub;

        #endregion

        #region Ctor

        public UfoBossRocketSeeking(
            Action<Construct> animateAction,
            Action<Construct> recycleAction)
        {
            ConstructType = ConstructType.UFO_BOSS_ROCKET_SEEKING;

            AnimateAction = animateAction;
            RecycleAction = recycleAction;

            _bomb_uris = Constants.CONSTRUCT_TEMPLATES.Where(x => x.ConstructType == ConstructType.UFO_BOSS_ROCKET_SEEKING).Select(x => x.Uri).ToArray();
            _bomb_blast_uris = Constants.CONSTRUCT_TEMPLATES.Where(x => x.ConstructType == ConstructType.BLAST).Select(x => x.Uri).ToArray();

            SetConstructSize();

            var uri = ConstructExtensions.GetRandomContentUri(_bomb_uris);
            _bitmapImage = new BitmapImage(uriSource: uri);

            _content_image = new()
            {
                Source = _bitmapImage,
                Height = this.Height,
                Width = this.Width,
                
            };

            SetChild(_content_image);

            //BorderThickness = new Microsoft.UI.Xaml.Thickness(Constants.DEFAULT_BLAST_RING_BORDER_THICKNESS);
            //CornerRadius = new Microsoft.UI.Xaml.CornerRadius(Constants.DEFAULT_BLAST_RING_CORNER_RADIUS);

            Speed = Constants.DEFAULT_CONSTRUCT_SPEED;
            IsometricDisplacement = Constants.DEFAULT_ISOMETRIC_DISPLACEMENT;
            DropShadowDistance = Constants.DEFAULT_DROP_SHADOW_DISTANCE;

            _audioStub = new AudioStub((SoundType.SEEKER_ROCKET_LAUNCH, 0.3, false), (SoundType.ROCKET_BLAST, 1, false));
        }

        #endregion
        #region Properties

        public bool IsBlasting { get; set; }

        #endregion
        #region Methods

        public void Reset()
        {
            _audioStub.Play(SoundType.SEEKER_ROCKET_LAUNCH);

            Opacity = 1;

            var uri = ConstructExtensions.GetRandomContentUri(_bomb_uris);
            _bitmapImage.UriSource = uri;

            //BorderBrush = new SolidColorBrush(Colors.Transparent);

            SetScaleTransform(1);
            SetRotation(0);

            IsBlasting = false;
            _autoBlastDelay = _autoBlastDelayDefault;
        }

        public void Reposition(UfoBoss UfoBoss)
        {
            SetPosition(
                left: (UfoBoss.GetLeft() + UfoBoss.Width / 2) - Width / 2,
                top: UfoBoss.GetBottom() - (40));
        }

        public bool AutoBlast()
        {
            _autoBlastDelay -= 0.1;

            if (_autoBlastDelay <= 0)
                return true;

            return false;
        }

        public void SetBlast()
        {
            _audioStub.Play(SoundType.ROCKET_BLAST);

            SetRotation(0);
            SetScaleTransform(Constants.DEFAULT_BLAST_SHRINK_SCALE);

            //BorderBrush = new SolidColorBrush(Colors.Goldenrod);

            var uri = ConstructExtensions.GetRandomContentUri(_bomb_blast_uris);
            _bitmapImage.UriSource = uri;
            IsBlasting = true;
        }

        #endregion
    }
}
