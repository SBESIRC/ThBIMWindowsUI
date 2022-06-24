using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace GLFW
{
	public class MouseState
    { /// <summary>
      /// The maximum number of buttons a <see cref="MouseState"/> can represent.
      /// </summary>
        internal const int MaxButtons = 16;

        private readonly BitArray _buttons = new BitArray(MaxButtons);
        private readonly BitArray _buttonsPrevious = new BitArray(MaxButtons);

        internal MouseState()
        {
        }

        private MouseState(MouseState source)
        {

            _buttons = (BitArray)source._buttons.Clone();
            _buttonsPrevious = (BitArray)source._buttonsPrevious.Clone();
        }


        /// <summary>
        /// Gets a <see cref="bool" /> indicating whether the specified
        ///  <see cref="MouseButton" /> is pressed.
        /// </summary>
        /// <param name="button">The <see cref="MouseButton" /> to check.</param>
        /// <returns><c>true</c> if key is pressed; <c>false</c> otherwise.</returns>
        public bool this[MouseButton button]
        {
            get => _buttons[(int)button];
            internal set { _buttons[(int)button] = value; }
        }
        /// <summary>
        /// Gets a value indicating whether any button is down.
        /// </summary>
        /// <value><c>true</c> if any button is down; otherwise, <c>false</c>.</value>
        public bool IsAnyButtonDown
        {
            get
            {
                for (int i = 0; i < MaxButtons; i++)
                {
                    if (_buttons[i])
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        internal void Update()
        {
            _buttonsPrevious.SetAll(false);
            _buttonsPrevious.Or(_buttons);
        }

        /// <summary>
        /// Gets a <see cref="bool" /> indicating whether this button is down.
        /// </summary>
        /// <param name="button">The <see cref="MouseButton" /> to check.</param>
        /// <returns><c>true</c> if the <paramref name="button"/> is down, otherwise <c>false</c>.</returns>
        public bool IsButtonDown(MouseButton button)
        {
            return _buttons[(int)button];
        }

        /// <summary>
        /// Gets a <see cref="bool" /> indicating whether this button was down in the previous frame.
        /// </summary>
        /// <param name="button">The <see cref="MouseButton" /> to check.</param>
        /// <returns><c>true</c> if the <paramref name="button"/> is down, otherwise <c>false</c>.</returns>
        public bool WasButtonDown(MouseButton button)
        {
            return _buttonsPrevious[(int)button];
        }

		/// <summary>
		/// Gets whether the specified mouse button is pressed in the current frame but released in the previous frame.
		/// </summary>
		/// <remarks>
		/// </remarks>
		/// <param name="button">The <see cref="MouseButton">mouse button</see> to check.</param>
		/// <returns>True if the mouse button is pressed in this frame, but not the last frame.</returns>
		public bool IsButtonPressed(MouseButton button)
        {
            return _buttons[(int)button] && !_buttonsPrevious[(int)button];
        }

        /// <summary>
        /// Gets whether the specified mouse button is released in the current frame but pressed in the previous frame.
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <param name="button">The <see cref="MouseButton">mouse button</see> to check.</param>
        /// <returns>True if the mouse button is released in this frame, but pressed the last frame.</returns>
        public bool IsButtonReleased(MouseButton button)
        {
            return !_buttons[(int)button] && _buttonsPrevious[(int)button];
        }

        /// <summary>
        /// Gets an immutable snapshot of this MouseState.
        /// This can be used to save the current mouse state for comparison later on.
        /// </summary>
        /// <returns>Returns an immutable snapshot of this MouseState.</returns>
        public MouseState GetSnapshot() => new MouseState(this);
    }
}
