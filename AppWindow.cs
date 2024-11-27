using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using SFML.Graphics;
using SFML.System;
using SFML.Window;
using static SFML.Window.Keyboard;

namespace KeyOverlay
{
    public class AppWindow
    {
        private RenderWindow _window;
        private Vector2u _size;

        private List<Key> _keyList;
        private List<int> _keyPressFadeList;
        private int _keyFadeTime;
        private float _keyFadeExp;

        private List<RectangleShape> _squareList;

        private float _barSpeed;
        private float _ratioX;
        private float _ratioY;
        private int _outlineThickness;
        private Color _backgroundColor;
        private Color _fontColor;
        private bool _fading;
        private bool _counter;
        private List<Drawable> _staticDrawables;
        private uint _maxFPS;
        private Clock _clock = new();
        private Config _config;
        private object _lock = new object();
        private FadingTexture _fadingTexture;

        public AppWindow()
        {
            _window = new RenderWindow(new VideoMode(600, 800), "Key Overlay");
            _config = new Config("config.ini", Initialize);
            Initialize();
        }

        public void Initialize()
        {
            lock (_lock)
            {
                var general = _config["General"];

                _barSpeed = float.Parse(general["barSpeed"], CultureInfo.InvariantCulture);
                _backgroundColor = CreateItems.CreateColor(general["backgroundColor"]);
                _maxFPS = uint.Parse(general["fps"]);

				/*
                _keyFadeTime = int.Parse(general["keyFadeTime"]);
                _keyFadeExp = float.Parse(general["keyFadeExp"]);
                if (_keyFadeTime < 0)
                    _keyFadeTime = 1;
                if (_keyFadeExp < 1f)
                    _keyFadeExp = 1f;
                */
				string keyFadeTimeStr;
                if (!general.TryGetValue("keyFadeTime", out keyFadeTimeStr))
                    _keyFadeTime = 7;
                else
                    _keyFadeTime = int.Parse(general["keyFadeTime"]);
                _keyFadeExp = 1.0f;

				//create keys which will be used to create the squares and text
				_keyList = new List<Key>();
                _keyPressFadeList = new List<int>();
                foreach (var item in _config["Keys"])
                {
                    var key = new Key(item.Value);

                    if (_config["Display"].ContainsKey(item.Key))
                        key.setKeyLetter(_config["Display"][item.Key]);

                    if (_config["Colors"].ContainsKey(item.Key))
                        key.setColor(CreateItems.CreateColor(_config["Colors"][item.Key]));

                    if (_config["Size"].ContainsKey(item.Key))
                        key.setSize(uint.Parse(_config["Size"][item.Key]));

                    _keyList.Add(key);
                    _keyPressFadeList.Add(0);
                }

                //create squares and add them to _staticDrawables list
                _outlineThickness = int.Parse(general["outlineThickness"]);
                var keySize = int.Parse(general["keySize"]);
                var margin = int.Parse(general["margin"]);

                var windowWidth = margin;
                foreach (var key in _keyList)
                {
                    windowWidth += keySize * (int) key._size + _outlineThickness * 2 + margin;
                }

                var windowHeight = int.Parse(general["height"]);
                _size = new Vector2u((uint) windowWidth, (uint) windowHeight);

                //calculate screen ratio relative to original program size for easy resizing
                _ratioX = windowWidth / 480f;
                _ratioY = windowHeight / 960f;

                _staticDrawables = new List<Drawable>();
                _squareList = CreateItems.CreateKeys(_keyList, keySize, _ratioX, _ratioY, margin, _outlineThickness);
                foreach (var square in _squareList) _staticDrawables.Add(square);

                // //create text and add it to _staticDrawables list
                _fontColor = new Color(255, 255, 255, 255);
                for (var i = 0; i < _keyList.Count; i++)
                {
                    var text = CreateItems.CreateText(_keyList[i].KeyLetter, _squareList[i], _fontColor, false);
                    _staticDrawables.Add(text);
                }

                if (general["fading"] == "yes")
                    _fading = true;
                if (general["counter"] == "yes")
                    _counter = true;

                _fadingTexture = new FadingTexture(_backgroundColor, _size.X, _ratioY);
            }
        }

        private void OnClose(object sender, EventArgs e)
        {
            _window.Close();
        }

        public void Run()
        {
            _window.Closed += OnClose;
            _window.SetFramerateLimit(_maxFPS);

            while (_window.IsOpen)
            {
                _window.DispatchEvents();

                _window.Size = _size;
                _window.SetView(new View(new FloatRect(0, 0, _size.X, _size.Y)));
                
                _window.Clear(_backgroundColor);
                
                lock (_lock) {
                    // //if no keys are being held fill the square with bg color
                    for (var i = 0; i < _keyList.Count; i++)
                    {
                        var key = _keyList[i];

                        if (key.isKey && Keyboard.IsKeyPressed(key.KeyboardKey) ||
                                !key.isKey && Mouse.IsButtonPressed(key.MouseButton))
                        {
                            key.Hold++;

                            _keyPressFadeList[i] = _keyFadeTime;
                            _squareList[i].FillColor = key._colorPressed;
                        }
                        else
                        {
                            key.Hold = 0;

							float fadeFactor = (float)Math.Pow((float)_keyPressFadeList[i] / (float)_keyFadeTime, _keyFadeExp);
							byte red = (byte)((float)_backgroundColor.R + ((float)key._colorPressed.R - (float)_backgroundColor.R) * fadeFactor);
							byte grn = (byte)((float)_backgroundColor.G + ((float)key._colorPressed.G - (float)_backgroundColor.G) * fadeFactor);
							byte blu = (byte)((float)_backgroundColor.B + ((float)key._colorPressed.B - (float)_backgroundColor.B) * fadeFactor);
							byte alp = (byte)((float)_backgroundColor.A + ((float)key._colorPressed.A - (float)_backgroundColor.A) * fadeFactor);

							Color _colorFaded = new Color(red, grn, blu, alp);
							_squareList[i].FillColor = _colorFaded;

                            if (_keyPressFadeList[i] > 0)
                                _keyPressFadeList[i]--;
                        }
                    }

                    MoveBars(_keyList, _squareList);

                    foreach (var staticDrawable in _staticDrawables) _window.Draw(staticDrawable);

                    for (var i = 0; i < _keyList.Count; i++)
                    {
                        var key = _keyList[i];

                        if (_counter)
                        {
                            var text = CreateItems.CreateText(
                                Convert.ToString(key.Counter),
                                    _squareList[i],
                                    Color.White,
                                    true
                                );
                            _window.Draw(text);
                        }
                        foreach (var bar in key.BarList) _window.Draw(bar);
                    }
                    _window.Draw(_fadingTexture.GetSprite());
                }
                _window.Display();
            }
        }

        /// <summary>
        /// if a key is a new input create a new bar, if it is being held stretch it and move all bars up
        /// </summary>
        private void MoveBars(List<Key> keyList, List<RectangleShape> squareList)
        {
            var moveDist = _clock.Restart().AsSeconds() * _barSpeed;

            foreach (var key in keyList)
            {
                if (key.Hold == 1)
                {
                    var rect = CreateItems.CreateBar(squareList.ElementAt(keyList.IndexOf(key)), _outlineThickness,
                        moveDist);
                    key.BarList.Add(rect);
                    key.Counter++;
                }
                else if (key.Hold > 1)
                {
                    var rect = key.BarList.Last();
                    rect.Size = new Vector2f(rect.Size.X, rect.Size.Y + moveDist);
                }

                foreach (var rect in key.BarList)
                    rect.Position = new Vector2f(rect.Position.X, rect.Position.Y - moveDist);
                if (key.BarList.Count > 0 && key.BarList.First().Position.Y + key.BarList.First().Size.Y < 0)
                    key.BarList.RemoveAt(0);
            }
        }
    }
}
