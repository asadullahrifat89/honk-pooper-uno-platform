﻿using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml;
using Microsoft.UI;
using System;
using System.Linq;

namespace HonkTrooper
{
    public partial class DropShadow : MovableConstruct
    {
        #region Properties

        public Construct ParentConstruct { get; set; }

        public double ParentConstructSpeed { get; set; } = 0;

        #endregion

        #region Ctor

        public DropShadow(
            Func<Construct, bool> animateAction,
            Func<Construct, bool> recycleAction)
        {
            ConstructType = ConstructType.DROP_SHADOW;

            var size = Constants.CONSTRUCT_SIZES.FirstOrDefault(x => x.ConstructType == ConstructType.DROP_SHADOW);

            var width = size.Width;
            var height = size.Height;

            AnimateAction = animateAction;
            RecycleAction = recycleAction;

            Height = 25;

            SetSize(width: width, height: height);

            Background = new SolidColorBrush(Colors.Black);
            CornerRadius = new CornerRadius(100);
            Opacity = 0.3;

            //SetSkewY(42);
            //SetRotation(100);

            SpeedOffset = Constants.DEFAULT_SPEED_OFFSET;
            IsometricDisplacement = Constants.DEFAULT_ISOMETRIC_DISPLACEMENT;
        }

        #endregion

        #region Methods

        public void SetParent(Construct construct)
        {
            Id = construct.Id;
            ParentConstruct = construct;
        }

        public void Reset()
        {
            SetPosition(
                left: (ParentConstruct.GetLeft() + ParentConstruct.Width / 2) - Width / 2,
                top: ParentConstruct.GetBottom() + (ParentConstruct.DropShadowDistance));

            if (Width != ParentConstruct.Width * 0.6)
                Width = ParentConstruct.Width * 0.6;
        }

        public void Move()
        {
            SetLeft((ParentConstruct.GetLeft() + ParentConstruct.Width / 2) - Width / 2);

            if (ParentConstruct.IsGravitatingDownwards)
            {
                MoveDownRight(ParentConstructSpeed * IsometricDisplacement);

                if (Width < ParentConstruct.Width)
                    Width += 0.5;
            }
            else if (ParentConstruct.IsGravitatingUpwards)
            {
                MoveDownRight(ParentConstructSpeed);

                if (Width > 0)
                    Width -= 0.5;
            }
            else
            {
                SetTop(ParentConstruct.GetBottom() + ParentConstruct.DropShadowDistance); // in normal circumstances, follow the parent
            }
        }

        #endregion
    }
}
