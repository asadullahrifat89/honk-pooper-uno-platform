﻿using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Linq;

namespace HonkTrooper
{
    public partial class TitleScreen : HoveringTitleScreen
    {
        #region Fields       

        private readonly TextBlock _titleScreenText;
        private readonly Image _content_image;

        private readonly Random _random;

        #endregion

        #region Ctor

        public TitleScreen
            (Func<Construct, bool> animateAction,
            Func<Construct, bool> recycleAction,
            Func<bool> playAction,
            double downScaling)
        {
            ConstructType = ConstructType.TITLE_SCREEN;

            _random = new Random();

            var size = Constants.CONSTRUCT_SIZES.FirstOrDefault(x => x.ConstructType == ConstructType.TITLE_SCREEN);

            var width = size.Width * downScaling;
            var height = size.Height * downScaling;

            SetSize(width: width, height: height);

            AnimateAction = animateAction;
            RecycleAction = recycleAction;

            CornerRadius = new CornerRadius(5);

            IsometricDisplacement = 0.5;
            SpeedOffset = Constants.DEFAULT_SPEED_OFFSET;
            DropShadowDistance = Constants.DEFAULT_DROP_SHADOW_DISTANCE;

            Grid grid = new();
            grid.Children.Add(new Border()
            {
                Background = new SolidColorBrush(Colors.Goldenrod),
                CornerRadius = new CornerRadius(15),
                Opacity = 0.6,
                BorderBrush = new SolidColorBrush(Colors.White),
                BorderThickness = new Thickness(Constants.DEFAULT_CONTROLLER_KEY_BORDER_THICKNESS),
            });

            StackPanel container = new()
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };

            var playerUris = Constants.CONSTRUCT_TEMPLATES.Where(x => x.ConstructType == ConstructType.PLAYER).Select(x => x.Uri).ToArray();

            var uri = ConstructExtensions.GetRandomContentUri(playerUris);

            _content_image = new Image()
            {
                Source = new BitmapImage(uriSource: uri),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Stretch = Stretch.Uniform,
                Margin = new Thickness(0, 0, 0, 5),
                Height = 110,
                Width = 110,
            };

            container.Children.Add(_content_image);

            _titleScreenText = new TextBlock()
            {
                Text = "Honk Trooper",
                FontSize = Constants.DEFAULT_GUI_FONT_SIZE,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 0, 0, 5),
                Foreground = new SolidColorBrush(Colors.White),
            };

            container.Children.Add(_titleScreenText);

            Button playButton = new()
            {
                Background = new SolidColorBrush(Colors.Goldenrod),
                Height = Constants.DEFAULT_CONTROLLER_KEY_SIZE,
                CornerRadius = new CornerRadius(Constants.DEFAULT_CONTROLLER_KEY_CORNER_RADIUS),
                Content = new SymbolIcon()
                {
                    Symbol = Symbol.Play,
                },
                BorderBrush = new SolidColorBrush(Colors.White),
                BorderThickness = new Thickness(Constants.DEFAULT_CONTROLLER_KEY_BORDER_THICKNESS),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Foreground = new SolidColorBrush(Colors.White),
            };

            playButton.Click += (s, e) => { playAction(); };

            container.Children.Add(playButton);

            grid.Children.Add(container);

            SetChild(grid);
        }

        #endregion      

        #region Methods

        public void Reposition()
        {
            SetPosition(
                  left: ((Scene.Width / 4) * 2) - Width / 2,
                  top: (Scene.Height / 2) - Height / 2,
                  z: 10);
        }

        public void SetContent(Uri uri)
        {
            _content_image.Source = new BitmapImage(uri);
        }

        public void SetTitle(string title)
        {
            _titleScreenText.Text = title;
        }

        #endregion
    }
}
