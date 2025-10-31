using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace AutoTransferShelves
{
    public class Building_LinkedStorage : Building_Storage
    {
        public Building_LinkedStorage LinkedOutputShelf;
        public bool AutoTransferEnabled = true;
        private const int DefaultTransferInterval = 60;
        public int ScheduledTransferInterval = DefaultTransferInterval;
        public bool IsLinked => LinkedOutputShelf != null && LinkedOutputShelf.Spawned;
        private CompPowerTrader powerComp;
        private bool isPreviouslyLinkedAndPowered = false;
        private bool previouslyFailedToTransfer = false;
        private Effecter activeEffecter;
        private Effecter outputEffecter;
        public bool AllowOverflow = false;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref LinkedOutputShelf, "LinkedOutputShelf");
            Scribe_Values.Look(ref AutoTransferEnabled, "AutoTransferEnabled", true);
            Scribe_Values.Look(ref ScheduledTransferInterval, "ScheduledTransferInterval", DefaultTransferInterval);
            Scribe_Values.Look(ref AllowOverflow, "AllowOverflow", false);
        }

        private int CalculateDistanceToLinkedShelf()
        {
            if (LinkedOutputShelf == null || !LinkedOutputShelf.Spawned)
                return 0;

            IntVec3 inputPosition = this.Position;
            IntVec3 outputPosition = LinkedOutputShelf.Position;
            return Mathf.Abs(inputPosition.x - outputPosition.x) + Mathf.Abs(inputPosition.z - outputPosition.z);
        }

        public override void Notify_ReceivedThing(Thing newItem)
        {
            base.Notify_ReceivedThing(newItem);

            if (this.def.defName == "OutputShelf")
            {
                if (outputEffecter == null)
                {
                    outputEffecter = DefDatabase<EffecterDef>.GetNamed("CustomBlastEMPEffect").Spawn();
                    outputEffecter.Trigger(this, this);
                }

                if (outputEffecter != null)
                {
                    LongEventHandler.ExecuteWhenFinished(() =>
                    {
                        outputEffecter?.Cleanup();
                        outputEffecter = null;
                    });
                }
            }
        }

        private void CleanupOutputEffecter()
        {
            if (outputEffecter != null)
            {
                outputEffecter.Cleanup();
                outputEffecter = null;
            }
        }

        public void ReinitializePowerComp()
        {
            powerComp = GetComp<CompPowerTrader>();
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            powerComp = GetComp<CompPowerTrader>();

            if (LinkedOutputShelf != null)
            {
                LinkedOutputShelf.powerComp = LinkedOutputShelf.GetComp<CompPowerTrader>();
            }
        }

        public override void DrawExtraSelectionOverlays()
        {
            base.DrawExtraSelectionOverlays();

            if (this.def.defName == "InputShelf" && !IsLinked)
            {
                DrawUnlinkedIcon("");
            }
            else if (this.def.defName == "OutputShelf")
            {
                bool isLinked = Map.listerThings.AllThings.OfType<Building_LinkedStorage>()
                    .Any(shelf => shelf.LinkedOutputShelf == this);

                if (!isLinked)
                {
                    DrawUnlinkedIcon("");
                }
            }

            if (this.def.defName == "InputShelf" && IsLinked && LinkedOutputShelf != null)
            {
                GenDraw.DrawLineBetween(this.DrawPos, LinkedOutputShelf.DrawPos, SimpleColor.Green);
            }
            else if (this.def.defName == "OutputShelf")
            {
                foreach (var inputShelf in Map.listerThings.AllThings.OfType<Building_LinkedStorage>())
                {
                    if (inputShelf.LinkedOutputShelf == this)
                    {
                        GenDraw.DrawLineBetween(inputShelf.DrawPos, this.DrawPos, SimpleColor.Green);
                    }
                }
            }
        }

        private void DrawUnlinkedIcon(string message)
        {
            Vector3 drawPos = this.DrawPos + new Vector3(0f, 0.1f, 0f);
            Material material = MaterialPool.MatFrom("UI/Overlays/OutOfFuel", ShaderDatabase.MetaOverlay);

            Graphics.DrawMesh(
                MeshPool.plane10,
                drawPos,
                Quaternion.identity,
                material,
                0
            );

            if (Find.Selector.SingleSelectedThing == this)
            {
                Messages.Message(message, MessageTypeDefOf.NeutralEvent);
            }
        }

        private void DrawUnlinkedIcon()
        {
            Vector3 drawPos = this.DrawPos + new Vector3(0f, 0.1f, 0f);
            Material material = MaterialPool.MatFrom("UI/Overlays/OutOfFuel", ShaderDatabase.MetaOverlay);

            Graphics.DrawMesh(
                MeshPool.plane10,
                drawPos,
                Quaternion.identity,
                material,
                0
            );
        }

        public override string GetInspectString()
        {
            string baseString = base.GetInspectString().TrimEnd();

            if (this.def.defName == "InputShelf")
            {
                baseString += (string.IsNullOrEmpty(baseString) ? "" : "\n") +
                              (IsLinked
                                  ? "StatusLinkedToReceiverShelf".Translate(LinkedOutputShelf?.LabelCap ?? "Unknown")
                                  : "StatusNotLinkedToReceiverShelf".Translate());
            }
            else if (this.def.defName == "OutputShelf")
            {
                bool isLinked = Map.listerThings.AllThings.OfType<Building_LinkedStorage>()
                    .Any(shelf => shelf.LinkedOutputShelf == this);

                baseString += (string.IsNullOrEmpty(baseString) ? "" : "\n") +
                              (isLinked
                                  ? "StatusLinkedToSenderShelf".Translate()
                                  : "StatusNotLinkedToSenderShelf".Translate());
            }

            return baseString;
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var gizmo in base.GetGizmos())
                yield return gizmo;

            if (this.def.defName == "InputShelf")
            {
                yield return new Command_Action
                {
                    defaultLabel = "LinkReceiverShelfLabel".Translate(),
                    defaultDesc = "LinkReceiverShelfDesc".Translate(),
                    icon = TexCommand.GatherSpotActive,
                    action = delegate
                    {
                        var targetParams = new TargetingParameters
                        {
                            canTargetBuildings = true,
                            canTargetSelf = false,
                            validator = target =>
                                target.Thing is Building_LinkedStorage shelf &&
                                shelf != this &&
                                shelf.def.defName == "OutputShelf"
                        };

                        Find.Targeter.BeginTargeting(targetParams, target =>
                        {
                            LinkedOutputShelf = target.Thing as Building_LinkedStorage;
                            LinkedOutputShelf?.ReinitializePowerComp();
                            Messages.Message(
                                LinkedOutputShelf != null
                                    ? "MessageLinkedTo".Translate(LinkedOutputShelf.LabelCap)
                                    : "MessageNoShelfLinked".Translate(),
                                MessageTypeDefOf.PositiveEvent
                            );
                        });
                    }
                };

                yield return new Command_Toggle
                {
                    defaultLabel = "AllowOverflowLabel".Translate(),
                    defaultDesc = "AllowOverflowDesc".Translate(),
                    icon = TexCommand.ForbidOff,
                    isActive = () => AllowOverflow,
                    toggleAction = () =>
                    {
                        AllowOverflow = !AllowOverflow;
                        Messages.Message(
                            AllowOverflow
                                ? "MessageOverflowOn".Translate()
                                : "MessageOverflowOff".Translate(),
                            MessageTypeDefOf.PositiveEvent
                        );
                    }
                };

                yield return new Command_Toggle
                {
                    defaultLabel = "ToggleAutoTransferLabel".Translate(),
                    defaultDesc = "ToggleAutoTransferDesc".Translate(),
                    icon = TexCommand.ForbidOff,
                    isActive = () => AutoTransferEnabled,
                    toggleAction = () =>
                    {
                        AutoTransferEnabled = !AutoTransferEnabled;
                        Messages.Message(
                            AutoTransferEnabled
                                ? "MessageAutoTransferOn".Translate()
                                : "MessageAutoTransferOff".Translate(),
                            MessageTypeDefOf.PositiveEvent
                        );
                    }
                };

                yield return new Command_Action
                {
                    defaultLabel = "UnlinkReceiverShelfLabel".Translate(),
                    defaultDesc = "UnlinkReceiverShelfDesc".Translate(),
                    icon = TexCommand.ClearPrioritizedWork,
                    action = delegate
                    {
                        if (LinkedOutputShelf != null)
                        {
                            Messages.Message("MessageUnlinked".Translate(), MessageTypeDefOf.PositiveEvent);
                            LinkedOutputShelf = null;
                        }
                        else
                        {
                            Messages.Message("MessageNoShelfLinked".Translate(), MessageTypeDefOf.RejectInput);
                        }
                    }
                };
            }
        }

        public void TransferItems()
        {
            if (!IsLinked)
            {
                CleanupEffecter();
                isPreviouslyLinkedAndPowered = false;
                previouslyFailedToTransfer = false;
                return;
            }

            if ((powerComp == null || !powerComp.PowerOn) ||
                (LinkedOutputShelf.powerComp == null || !LinkedOutputShelf.powerComp.PowerOn))
            {
                CleanupEffecter();
                isPreviouslyLinkedAndPowered = false;
                previouslyFailedToTransfer = false;
                return;
            }

            if (!isPreviouslyLinkedAndPowered)
            {
                Messages.Message("MessageLinked".Translate(), MessageTypeDefOf.PositiveEvent);
                isPreviouslyLinkedAndPowered = true;
            }

            var inputGroup = this.GetSlotGroup();
            var outputGroup = LinkedOutputShelf.GetSlotGroup();

            if (inputGroup == null || outputGroup == null)
            {
                CleanupEffecter();
                previouslyFailedToTransfer = false;
                return;
            }

            var inputThings = inputGroup.HeldThings.ToList();
            if (inputThings.Count == 0)
            {
                CleanupEffecter();
                previouslyFailedToTransfer = false;
                return;
            }

            bool outputFull = outputGroup.HeldThings.Count() >= 3 && !AllowOverflow;
            if (outputFull)
            {
                CleanupEffecter();
                if (!previouslyFailedToTransfer)
                {
                    Messages.Message("MessageTransferFailed".Translate(), MessageTypeDefOf.RejectInput);
                    previouslyFailedToTransfer = true;
                }
                return;
            }

            // Start effect if needed
            if (activeEffecter == null)
            {
                activeEffecter = DefDatabase<EffecterDef>.GetNamed("CustomBlastEMPEffect").Spawn();
                activeEffecter.Trigger(this, this);
            }

            foreach (var item in inputThings)
            {
                if (!LinkedOutputShelf.GetStoreSettings().AllowedToAccept(item))
                    continue;

                bool transferred = false;

                foreach (var existing in outputGroup.HeldThings.Where(t => t.def == item.def))
                {
                    int space = existing.def.stackLimit - existing.stackCount;
                    if (space > 0)
                    {
                        int moveCount = Mathf.Min(item.stackCount, space);
                        var moved = item.SplitOff(moveCount);
                        existing.stackCount += moveCount;
                        transferred = true;
                        break;
                    }
                }

                if (!transferred)
                {
                    var newItem = item.SplitOff(Mathf.Min(item.stackCount, item.def.stackLimit));
                    if (outputGroup.HeldThings.Count() < 3 || AllowOverflow)
                    {
                        if (!GenPlace.TryPlaceThing(newItem, LinkedOutputShelf.Position, Map, ThingPlaceMode.Direct))
                        {
                            GenPlace.TryPlaceThing(newItem, LinkedOutputShelf.Position.RandomAdjacentCell8Way(), Map, ThingPlaceMode.Near);
                        }
                    }
                    else
                    {
                        item.stackCount += newItem.stackCount; // rollback
                    }
                }
            }

            previouslyFailedToTransfer = false;
        }

        // Cleanup method for the Effecter
        private void CleanupEffecter()
        {
            if (activeEffecter != null)
            {
                activeEffecter.Cleanup();
                activeEffecter = null;
            }
        }

        private int CountUniqueStacks(Building_LinkedStorage shelf)
        {
            var slotGroup = shelf.GetSlotGroup();
            return slotGroup?.HeldThings.Count() ?? 0;
        }

        protected override void Tick()
        {
            base.Tick();

            if (powerComp != null)
            {
                // Adjust power consumption based on distance
                int distance = CalculateDistanceToLinkedShelf();
                float basePower = 150f; // Base power consumption
                float additionalPowerPerCell = 5f; // Additional power consumption per cell of distance

                powerComp.PowerOutput = -1 * (basePower + distance * additionalPowerPerCell);

                if (!powerComp.PowerOn)
                    return;
            }

            if (AutoTransferEnabled && IsLinked && Find.TickManager.TicksGame % ScheduledTransferInterval == 0)
            {
                TransferItems();
            }
        }

    }
}
