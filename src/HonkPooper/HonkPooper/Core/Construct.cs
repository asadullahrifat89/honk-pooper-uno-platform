﻿using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using Windows.Foundation;

namespace HonkPooper
{
    public partial class Construct : Border
    {
        #region Fields

        private readonly CompositeTransform _compositeTransform = new()
        {
            CenterX = 0.5,
            CenterY = 0.5,
            Rotation = 0,
            ScaleX = 1,
            ScaleY = 1,
        };

        private bool _isPoppingComplete;
        private double _popUpScalingLimit = 1.5;

        #endregion

        #region Properties

        /// <summary>
        /// Unique id of the construct.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Type of the construct.
        /// </summary>
        public ConstructType ConstructType { get; set; }

        /// <summary>
        /// Animation function.
        /// </summary>
        public Func<Construct, bool> AnimateAction { get; set; }

        /// <summary>
        /// Recycling function.
        /// </summary>
        public Func<Construct, bool> RecycleAction { get; set; }

        /// <summary>
        /// Adds an offset while animating this construct with the scene speed.
        /// </summary>
        public double SpeedOffset { get; set; } = 0;

        /// <summary>
        /// Displacement value that determines isometric movement.
        /// </summary>
        public double IsometricDisplacement { get; set; }

        /// <summary>
        /// Returns true if faded.
        /// </summary>
        public bool IsFadingComplete => Opacity <= 0;

        /// <summary>
        /// Only animated by the scene if set to true.
        /// </summary>
        public bool IsAnimating { get; set; }

        /// <summary>
        /// The distance from this construct at which a shadow should appear.
        /// </summary>
        public double DropShadowDistance { get; set; }

        public bool IsGravitating { get; set; }

        /// <summary>
        /// Determines if pop effect should execute for this construct.
        /// </summary>
        public bool AwaitingPop { get; set; }

        #endregion

        #region Ctor

        public Construct()
        {
            RenderTransformOrigin = new Point(0.5, 0.5);

            RenderTransform = _compositeTransform;
            CanDrag = false;
        }

        public Construct(
            ConstructType constructType,
            double width,
            double height,
            Func<Construct, bool> animateAction,
            Func<Construct, bool> recycleAction,
            UIElement content = null,
            double speedOffset = 0)
        {
            ConstructType = constructType;

            AnimateAction = animateAction;
            RecycleAction = recycleAction;

            SpeedOffset = speedOffset;

            RenderTransformOrigin = new Point(0.5, 0.5);

            RenderTransform = _compositeTransform;
            CanDrag = false;

            SetSize(width: width, height: height);

            if (content is not null)
                SetChild(content);
        }

        #endregion

        #region Methods     

        public void SetAction(
            Func<Construct, bool> movementAction,
            Func<Construct, bool> recycleAction)
        {
            AnimateAction = movementAction;
            RecycleAction = recycleAction;
        }

        public void Animate()
        {
            AnimateAction(this);
        }

        public void Recycle()
        {
            RecycleAction(this);
        }

        public void SetSize(double width, double height)
        {
            Width = width;
            Height = height;
        }

        public double GetTop()
        {
            return Canvas.GetTop(this);
        }

        public double GetBottom()
        {
            return Canvas.GetTop(this) + Height;
        }

        public double GetLeft()
        {
            return Canvas.GetLeft(this);
        }

        public double GetRight()
        {
            return Canvas.GetLeft(this) + Width;
        }

        public int GetZ()
        {
            return Canvas.GetZIndex(this);
        }

        public void SetTop(double top)
        {
            Canvas.SetTop(this, top);
        }

        public void SetLeft(double left)
        {
            Canvas.SetLeft(this, left);
        }

        public void SetZ(int z)
        {
            Canvas.SetZIndex(this, z);
        }

        public void SetPosition(double left, double top)
        {
            Canvas.SetTop(this, top);
            Canvas.SetLeft(this, left);
        }

        public void SetPosition(double left, double top, int z)
        {
            Canvas.SetTop(this, top);
            Canvas.SetLeft(this, left);
            Canvas.SetZIndex(this, z);
        }

        public void SetScaleTransform(double scaleXY)
        {
            _compositeTransform.ScaleX = scaleXY;
            _compositeTransform.ScaleY = scaleXY;
        }

        public void SetScaleTransform(double scaleX, double scaleY)
        {
            _compositeTransform.ScaleX = scaleX;
            _compositeTransform.ScaleY = scaleY;
        }

        public double GetScaleX()
        {
            return _compositeTransform.ScaleX;
        }

        public double GetScaleY()
        {
            return _compositeTransform.ScaleY;
        }

        public void SetScaleX(double scaleX)
        {
            _compositeTransform.ScaleX = scaleX;
        }

        public void SetScaleY(double scaleY)
        {
            _compositeTransform.ScaleY = scaleY;
        }

        public void SetRotation(double rotation)
        {
            _compositeTransform.Rotation = rotation;
        }

        public void SetSkewX(double skewX)
        {
            _compositeTransform.SkewX = skewX;
        }

        public void SetSkewY(double skewY)
        {
            _compositeTransform.SkewY = skewY;
        }

        public void SetChild(UIElement uIElement)
        {
            Child = uIElement;
        }

        public void Fade()
        {
            Opacity -= 0.005;
        }

        public void Fade(double fade)
        {
            Opacity -= fade;
        }

        public void Shrink()
        {
            _compositeTransform.ScaleX -= 0.1;
            _compositeTransform.ScaleY -= 0.1;
        }

        public void Expand()
        {
            _compositeTransform.ScaleX += 0.1;
            _compositeTransform.ScaleY += 0.1;
        }

        public void Pop()
        {
            if (AwaitingPop)
            {
                if (!_isPoppingComplete && GetScaleX() < _popUpScalingLimit)
                    Expand();

                if (GetScaleX() >= _popUpScalingLimit)
                    _isPoppingComplete = true;

                if (_isPoppingComplete)
                {
                    Shrink();

                    if (GetScaleX() <= 1)
                    {
                        _isPoppingComplete = false;
                        AwaitingPop = false; // stop popping effect                        
                    }
                }
            }
        }

        #endregion
    }
}