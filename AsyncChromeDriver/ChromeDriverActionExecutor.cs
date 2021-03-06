// Copyright (c) Oleg Zudov. All Rights Reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Zu.WebBrowser.AsyncInteractions;
using Zu.WebBrowser.BasicTypes;
using Zu.AsyncWebDriver.Interactions;

namespace Zu.Chrome
{
    internal class ChromeDriverActionExecutor : IActionExecutor
    {
        private AsyncChromeDriver _asyncChromeDriver;
        private CancellationTokenSource _performActionsCancellationTokenSource;
        public ChromeDriverActionExecutor(AsyncChromeDriver asyncChromeDriver)
        {
            _asyncChromeDriver = asyncChromeDriver;
        }

        public Task<bool> IsActionExecutor(CancellationToken cancellationToken = default (CancellationToken))
        {
            return Task.FromResult(true);
        }

        public async Task PerformActions(IList<ActionSequence> actionSequenceList, CancellationToken cancellationToken = default (CancellationToken))
        {
            _performActionsCancellationTokenSource = new CancellationTokenSource();
            using (CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_performActionsCancellationTokenSource.Token, cancellationToken))
            {
                try
                {
                    var ct = linkedCts.Token;
                    ct.ThrowIfCancellationRequested();
                    foreach (var action in actionSequenceList)
                    {
                        ct.ThrowIfCancellationRequested();
                        cancellationToken.ThrowIfCancellationRequested();
                        foreach (var interaction in action.Interactions)
                        {
                            //await Task.Delay(100);
                            if (interaction is PauseInteraction)
                            {
                                await Task.Delay(((PauseInteraction)interaction).Duration, ct).ConfigureAwait(false);
                            }
                            else if (interaction is PointerInputDevice.PointerDownInteraction)
                            {
                                var pdi = (PointerInputDevice.PointerDownInteraction)interaction;
                                var pk = ((PointerInputDevice)interaction.SourceDevice).PointerKind;
                                if (pk == PointerKind.Mouse)
                                {
                                    if (pdi.Button == MouseButton.Left)
                                    {
                                        await _asyncChromeDriver.Mouse.MouseDown(_asyncChromeDriver.Session.MousePosition, ct).ConfigureAwait(false);
                                    }
                                    else if (pdi.Button == MouseButton.Right)
                                    {
                                        await _asyncChromeDriver.Mouse.ContextClick(_asyncChromeDriver.Session.MousePosition, ct).ConfigureAwait(false);
                                    }
                                }
                                else if (pk == PointerKind.Touch)
                                {
                                    if (pdi.Button == MouseButton.Left)
                                    {
                                        await _asyncChromeDriver.TouchScreen.Down(_asyncChromeDriver.Session.MousePosition.X, _asyncChromeDriver.Session.MousePosition.Y, ct).ConfigureAwait(false);
                                    }
                                    else if (pdi.Button == MouseButton.Right)
                                    {
                                        throw new NotSupportedException("Touch with MouseButton.Right");
                                    }
                                }
                                else if (pk == PointerKind.Pen)
                                {
                                    throw new NotImplementedException("PointerKind.Pen");
                                }
                            }
                            else if (interaction is PointerInputDevice.PointerUpInteraction)
                            {
                                var pui = (PointerInputDevice.PointerUpInteraction)interaction;
                                var pk = ((PointerInputDevice)interaction.SourceDevice).PointerKind;
                                if (pk == PointerKind.Mouse)
                                {
                                    if (pui.Button == MouseButton.Left)
                                    {
                                        await _asyncChromeDriver.Mouse.MouseUp(_asyncChromeDriver.Session.MousePosition, ct).ConfigureAwait(false);
                                    }
                                    else if (pui.Button == MouseButton.Right)
                                    {
                                        await _asyncChromeDriver.Mouse.ContextClick(_asyncChromeDriver.Session.MousePosition, ct).ConfigureAwait(false);
                                    }
                                }
                                else if (pk == PointerKind.Touch)
                                {
                                    throw new NotSupportedException("Touch with MouseButton.Right");
                                }
                                else if (pk == PointerKind.Pen)
                                {
                                    throw new NotImplementedException("PointerKind.Pen");
                                }
                            }
                            else if (interaction is PointerInputDevice.PointerCancelInteraction)
                            {
                            }
                            else if (interaction is PointerInputDevice.PointerMoveInteraction)
                            {
                                var pmi = (PointerInputDevice.PointerMoveInteraction)interaction;
                                var pk = ((PointerInputDevice)interaction.SourceDevice).PointerKind;
                                if (pk == PointerKind.Mouse)
                                {
                                    if (pmi.Target != null)
                                    {
                                        if (pmi.X != 0 || pmi.Y != 0)
                                        {
                                            WebPoint location = await pmi.Target.Location().ConfigureAwait(false);
                                            location = location.Offset(pmi.X, pmi.Y);
                                            await _asyncChromeDriver.Mouse.MouseMove(location, ct).ConfigureAwait(false);
                                        }
                                        else
                                        {
                                            //WebPoint location = await asyncChromeDriver.ElementUtils.GetElementClickableLocation(pmi.Target.Id, ct);
                                            //if (location == null) 
                                            var location = await _asyncChromeDriver.Elements.GetElementLocation(pmi.Target.Id, ct).ConfigureAwait(false);
                                            await _asyncChromeDriver.Mouse.MouseMove(location, ct).ConfigureAwait(false);
                                        }
                                    }
                                    else
                                        await _asyncChromeDriver.Mouse.MouseMove(_asyncChromeDriver.Session.MousePosition.Offset(pmi.X, pmi.Y), ct).ConfigureAwait(false);
                                }
                                else if (pk == PointerKind.Touch)
                                {
                                    if (pmi.Target != null)
                                    {
                                        if (pmi.X != 0 || pmi.Y != 0)
                                        {
                                            WebPoint location = await pmi.Target.Location().ConfigureAwait(false);
                                            location = location.Offset(pmi.X, pmi.Y);
                                            await _asyncChromeDriver.TouchScreen.Move(location.X, location.Y, ct).ConfigureAwait(false);
                                        }
                                        else
                                        {
                                            //WebPoint location = await asyncChromeDriver.ElementUtils.GetElementClickableLocation(pmi.Target.Id);
                                            var location = await _asyncChromeDriver.Elements.GetElementLocation(pmi.Target.Id, ct).ConfigureAwait(false);
                                            if (location != null)
                                                await _asyncChromeDriver.TouchScreen.Move(location.X, location.Y, ct).ConfigureAwait(false);
                                        }
                                    }
                                    else
                                    {
                                        var newLoc = _asyncChromeDriver.Session.MousePosition.Offset(pmi.X, pmi.Y);
                                        await _asyncChromeDriver.TouchScreen.Move(newLoc.X, newLoc.Y, ct).ConfigureAwait(false);
                                    }
                                }
                                else if (pk == PointerKind.Pen)
                                {
                                    throw new NotImplementedException("PointerKind.Pen");
                                }
                            }
                            else if (interaction is KeyInputDevice.KeyDownInteraction)
                            {
                                var value = ((KeyInputDevice.KeyDownInteraction)interaction).GetValue();
                                await _asyncChromeDriver.Keyboard.PressKey(value, ct).ConfigureAwait(false);
                            }
                            else if (interaction is KeyInputDevice.KeyUpInteraction)
                            {
                                var value = ((KeyInputDevice.KeyUpInteraction)interaction).GetValue();
                                await _asyncChromeDriver.Keyboard.ReleaseKey(value, ct).ConfigureAwait(false);
                            }
                        }
                    }
                }
                catch
                {
                    throw;
                }
            }
        }

        public Task ResetInputState(CancellationToken cancellationToken = default (CancellationToken))
        {
            return CancelCurrentActions();
        }

        private Task CancelCurrentActions()
        {
            _performActionsCancellationTokenSource?.Cancel();
            return Task.CompletedTask;
        }
    }
}