﻿


namespace CentaurWarrunner
{
    using System;
    using System.Linq;
    using System.Windows.Input;

    using Ensage;
    using Ensage.Common;
    using Ensage.Common.Extensions;

    using SharpDX;
    using SharpDX.Direct3D9;
    class CentaurWarrunner
    {
        #region Static Fields

        private static Item abyssalBlade;
        private static Item blink;
        private static float blinkRange;

        // Abilities
        private static Ability hoofStomp;
        private static bool enableQ = true;

        private static Ability doubleEdge;
        private static bool enableW = true;

        // Ultimate Ability
        private static Ability Stampede;

        // No clue what this is
        private static float hullsum;

        private static float lastActivity;

        private static float lastStack;

        private static bool loaded;

        private static Hero me;

        private static Vector3 mePosition;

        private static float nextAttack;

        private static Hero target;

        private static float targetDistance;

        private static Font text;

        private static double turnTime;

        #endregion

        #region Public Methods and Operators

        public static void Init()
        {
            Game.OnUpdate += Game_OnUpdate;
            loaded = false;

            text = new Font(
                Drawing.Direct3DDevice9,
                new FontDescription
                {
                    FaceName = "Tahoma",
                    Height = 13,
                    OutputPrecision = FontPrecision.Default,
                    Quality = FontQuality.Default
                }
                );

            Drawing.OnPreReset += Drawing_OnPreReset;
            Drawing.OnPostReset += Drawing_OnPostReset;
            Drawing.OnEndScene += Drawing_OnEndScene;
            AppDomain.CurrentDomain.DomainUnload += CurrentDomainDomainUnload;
            Game.OnWndProc += Game_OnWndProc;

        }
        #endregion

        private static bool CastCombo()
        {
            if (!Utils.SleepCheck("casting") || !me.CanCast() || !target.IsVisible)
            {
                return false;
            } // end if

            var casted = false;
            if (abyssalBlade != null && abyssalBlade.CanBeCasted() && targetDistance <= (350 + hullsum) &&
                Utils.SleepCheck("abyssal"))
            {
                var canUse = !target.IsStunned() && !target.IsHexed() && !target.IsInvul() && target.IsMagicImmune();

                if (canUse)
                {
                    abyssalBlade.UseAbility(target);
                    Utils.Sleep(turnTime * 1000 + 1000 + Game.Ping, "abyssal");
                    Utils.Sleep(turnTime * 1000 + 100, "move");
                    Utils.Sleep(turnTime * 1000 + 100, "casting");
                    casted = true;

                } // end if can use if statement


            } // end if  below casted


            if (blink != null && blink.CanBeCasted() && targetDistance > 400 && targetDistance < (blinkRange + hullsum)
               && Utils.SleepCheck("blink"))
            {
                var position = target.Position + target.Vector3FromPolarAngle() * (hullsum + me.AttackRange);
                if (mePosition.Distance(position) < targetDistance)
                {
                    position = target.Position;
                }
                var dist = position.Distance2D(mePosition);
                if (dist > blinkRange)
                {
                    position = (position - mePosition) * (blinkRange - 1) / position.Distance2D(me) + mePosition;
                }
                blink.UseAbility(position);
                mePosition = position;
                Utils.Sleep(turnTime * 1000 + 1000 + Game.Ping, "blink");
                Utils.Sleep(turnTime * 1000 + 100, "move");
                Utils.Sleep(turnTime * 1000 + 100, "casting");
                casted = true;
            }
            const int Radius = 300;
            var canAttack = !target.IsInvul() && !target.IsAttackImmune() && me.CanAttack();
            if (!canAttack)
            {
                return casted;
            }
            if (doubleEdge.CanBeCasted() && Utils.SleepCheck("W") && !(hoofStomp.CanBeCasted() && enableQ))
            {
                if (mePosition.Distance2D(target) <= (Radius + hullsum))
                {
                    doubleEdge.UseAbility(target);
                    Utils.Sleep(1000 + Game.Ping, "W");
                    Utils.Sleep(100, "casting");
                    casted = true;
                }
            }


   
            else if (hoofStomp.CanBeCasted())
            {
                if (mePosition.Distance2D(target) <= (Radius + hullsum + 100))
                {
                    hoofStomp.UseAbility();
                    Utils.Sleep(1000 + Game.Ping, "Q");
                    Utils.Sleep(100, "casting");
                    casted = true;
                }
                else
                {
                    var pos = target.Position
                          + target.Vector3FromPolarAngle() * ((Game.Ping / 1000 + 0.3f) * target.MovementSpeed);               
                    me.Move(pos);
                    Utils.Sleep(200, "moveCloser");
                    casted = false;
                }
            }
            


            if (!Stampede.CanBeCasted() || !Utils.SleepCheck("R"))
            {
                return casted;
            }
            if (!(mePosition.Distance2D(target) <= (Radius + hullsum)))
            {
                return casted;
            }
            Stampede.UseAbility();
            Utils.Sleep(1000 + Game.Ping, "R");
            Utils.Sleep(100, "casting");
            return casted;
        }

        private static void CurrentDomainDomainUnload(object sender, EventArgs e)
        {
            text.Dispose();
        }

        private static void Drawing_OnEndScene(EventArgs args)
        {
            if (Drawing.Direct3DDevice9 == null || Drawing.Direct3DDevice9.IsDisposed || !Game.IsInGame)
            {
                return;
            }

            var player = ObjectMgr.LocalPlayer;
            if (player == null || player.Team == Team.Observer)
            {
                return;
            }

            text.DrawText(
                null,
                enableQ ? "Centaur Warrunner: Combo - DISABLED! | [G] for toggle" : "Centaur Warrunner: Combo - ENABLED!! | [G] for toggle",
                5,
                96,
                Color.Green);
        }

        private static void Drawing_OnPostReset(EventArgs args)
        {
            text.OnResetDevice();
        }

        private static void Drawing_OnPreReset(EventArgs args)
        {
            text.OnLostDevice();
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            if (!loaded)
            {
                me = ObjectMgr.LocalHero;
                if (!Game.IsInGame || me == null)
                {
                    return;
                }
                hoofStomp = me.Spellbook.Spell1;
                doubleEdge = me.Spellbook.SpellW;
                Stampede = me.FindSpell("centaur_stampede");
                blink = me.FindItem("item_blink");
                abyssalBlade = me.FindItem("item_abyssal_blade");
                lastStack = 0;
                loaded = true;
                lastActivity = 0;
            }

            if (!Game.IsInGame || me == null)
            {
                loaded = false;
                return;
            }

            if (Game.IsPaused)
            {
                return;
            }

            var tick = Environment.TickCount;
            if (me.NetworkActivity != (NetworkActivity)lastActivity && target != null)
            {
                lastActivity = (float)me.NetworkActivity;
                if (lastActivity == 1503)
                {
                    nextAttack = (tick + me.SecondsPerAttack * 1000 - Game.Ping);
                }
            }

            if (blink == null)
            {
                blink = me.FindItem("item_blink");
            }

            if (abyssalBlade == null)
            {
                abyssalBlade = me.FindItem("item_abyssal_blade");
            }

            if (!Game.IsKeyDown(Key.Space) || (Game.IsChatOpen))
            {
                target = null;
                lastStack = 0;
                return;
            }
            if (Utils.SleepCheck("blink"))
            {
                mePosition = me.Position;
            }
            var range = 1000f;
            var mousePosition = Game.MousePosition;
            if (blink != null)
            {
                blinkRange = blink.AbilityData.FirstOrDefault(x => x.Name == "blink_range").GetValue(0);
                range = blinkRange + me.HullRadius + 200;
            }
            var lastTarget = target;
            target = me.ClosestToMouseTarget(range);
            if (!Equals(target, lastTarget))
            {
                lastStack = 0;
            }
            if (target == null || !target.IsAlive || !target.IsVisible
                || target.Distance2D(mousePosition) > target.Distance2D(me) + 1000)
            {
                if (!Utils.SleepCheck("move"))
                {
                    return;
                }
                me.Move(mousePosition);
                Utils.Sleep(100, "move");
                return;
            }
            targetDistance = mePosition.Distance2D(target);
            hullsum = (me.HullRadius + target.HullRadius) * 2;
            turnTime = me.GetTurnTime(target);
            var casting = CastCombo();
            if (casting)
            {
                return;
            }
            OrbWalk(tick);
        }

        private static void Game_OnWndProc(WndEventArgs args)
        {
            if (args.Msg != (ulong)Utils.WindowsMessages.WM_KEYUP || args.WParam != 'G' || Game.IsChatOpen)
            {
                return;
            }
            enableQ = !enableQ;
            enableW = !enableW;
        }

        private static void OrbWalk(float tick)
        {
            var modifier = target.Modifiers.FirstOrDefault(x => x.Name == "modifier_centaur_return_damage_increase");
            var stackCount = lastStack;
            var currentStack = modifier != null ? modifier.StackCount : 0;
            var notAttacking = (stackCount < currentStack) || targetDistance > (hullsum + 300);
            var canAttack = ((nextAttack - Game.Ping - turnTime) <= tick) && !target.IsInvul()
                            && !target.IsAttackImmune() && me.CanAttack();
            if (canAttack)
            {
                if (!Utils.SleepCheck("attack"))
                {
                    return;
                }
                me.Attack(target);
                lastStack = modifier != null ? modifier.StackCount : 0;
                Utils.Sleep(100, "attack");
                return;
            }
            if (!Utils.SleepCheck("move") || !notAttacking
                || ((!target.CanMove() || target.NetworkActivity == (NetworkActivity)1500 || target.MovementSpeed < 200)
                    && targetDistance < 200))
            {
                return;
            }
            var mousePos = Game.MousePosition;
            if (mousePos.Distance2D(target) > 400)
            {
                me.Follow(target);
            }
            else
            {
                me.Move(mousePos);
            }
            Utils.Sleep(100, "move");
        }

    }
}

