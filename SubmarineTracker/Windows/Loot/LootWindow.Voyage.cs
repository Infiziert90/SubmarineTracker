using SubmarineTracker.Data;
using static SubmarineTracker.Data.Loot;

namespace SubmarineTracker.Windows.Loot;

public partial class LootWindow
{
    private static readonly int MaxLength = "Craftsman's Command Mat".Length;

    private void VoyageTab()
    {
        if (ImGui.BeginTabItem("Voyage"))
        {
            var existingSubs = Submarines.KnownSubmarines.Values
                                         .SelectMany(fc => fc.Submarines.Select(s => $"{Helper.GetFCName(fc)} - {s.Name} ({s.Build.FullIdentifier()})"))
                                         .ToArray();
            if (!existingSubs.Any())
            {
                Helper.NoData();
                ImGui.EndTabItem();
                return;
            }

            var selectedSubmarine = SelectedSubmarine;
            ImGui.Combo("##existingSubs", ref selectedSubmarine, existingSubs, existingSubs.Length);
            Helper.DrawArrows(ref selectedSubmarine, existingSubs.Length, 1);

            if (selectedSubmarine != SelectedSubmarine)
            {
                SelectedSubmarine = selectedSubmarine;
                SelectedVoyage = 0;
            }

            var selectedSub = Submarines.KnownSubmarines.Values.SelectMany(fc => fc.Submarines).ToList()[SelectedSubmarine];

            var fc = Submarines.KnownSubmarines.Values.First(fcLoot => fcLoot.SubLoot.Values.Any(loot => loot.Loot.ContainsKey(selectedSub.Return)));
            var submarineLoot = fc.SubLoot.Values.First(loot => loot.Loot.ContainsKey(selectedSub.Return));

            var submarineVoyage = submarineLoot.Loot
                                               .SkipLast(1)
                                               .Where(pair => !Configuration.ExcludeLegacy || pair.Value.First().Valid)
                                               .Reverse()
                                               .Select(pair => $"{pair.Value.First().Date}")
                                               .ToArray();
            if (!submarineVoyage.Any())
            {
                ImGui.TextColored(ImGuiColors.ParsedOrange, "Tracking starts when you send your subs on voyage again.");

                ImGui.EndTabItem();
                return;
            }

            ImGui.Combo("##voyageSelection", ref SelectedVoyage, submarineVoyage, submarineVoyage.Length);
            Helper.DrawArrows(ref SelectedVoyage, submarineVoyage.Length, 2);

            ImGuiHelpers.ScaledDummy(5.0f);

            var loot = submarineLoot.Loot.SkipLast(1).Reverse().ToArray()[SelectedVoyage];
            var stats = loot.Value.First();
            if (stats.Valid)
                ImGui.TextColored(ImGuiColors.TankBlue, $"Rank: {stats.Rank} SRF: {stats.Surv}, {stats.Ret}, {stats.Fav}");
            else
                ImGui.TextColored(ImGuiColors.ParsedOrange, "-Legacy Data-");

            ImGuiHelpers.ScaledDummy(5.0f);

            foreach (var detailedLoot in loot.Value)
            {
                var primaryItem = ItemSheet.GetRow(detailedLoot.Primary)!;
                var additionalItem = ItemSheet.GetRow(detailedLoot.Additional)!;

                ImGui.TextColored(ImGuiColors.HealerGreen, ExplorationSheet.GetRow(detailedLoot.Sector)!.ConvertDestination());
                ImGui.Indent(10.0f);
                if (stats.Valid)
                    ImGui.TextUnformatted($"DD: {ProcToText(detailedLoot.FavProc)} --- Ret: {ProcToText(detailedLoot.PrimaryRetProc)}");

                if (ImGui.BeginTable($"##VoyageLootTable", 4))
                {
                    ImGui.TableSetupColumn("##icon", 0, 0.2f);
                    ImGui.TableSetupColumn("##item");
                    ImGui.TableSetupColumn("##amount", 0, 0.2f);
                    ImGui.TableSetupColumn("##survProc", 0, 0.4f);

                    ImGui.TableNextColumn();
                    Helper.DrawScaledIcon(primaryItem.Icon, IconSize);
                    ImGui.TableNextColumn();

                    var name = Utils.ToStr(primaryItem.Name);
                    if (MaxLength < name.Length)
                        name = name.Truncate(MaxLength);
                    ImGui.TextUnformatted(name);
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip(Utils.ToStr(primaryItem.Name));

                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted($"x{detailedLoot.PrimaryCount}");
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted($"{(stats.Valid ? ProcToText(detailedLoot.PrimarySurvProc) : "")}");
                    ImGui.TableNextRow();

                    if (detailedLoot.ValidAdditional)
                    {
                        ImGui.TableNextColumn();
                        Helper.DrawScaledIcon(additionalItem.Icon, IconSize);
                        ImGui.TableNextColumn();

                        name = Utils.ToStr(additionalItem.Name);
                        if (MaxLength < name.Length)
                            name = name.Truncate(MaxLength);
                        ImGui.TextUnformatted(name);
                        if (ImGui.IsItemHovered())
                            ImGui.SetTooltip(Utils.ToStr(additionalItem.Name));

                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted($"x{detailedLoot.AdditionalCount}");
                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted($"{(stats.Valid ? ProcToText(detailedLoot.AdditionalSurvProc) : "")}");
                        ImGui.TableNextRow();
                    }

                    ImGui.EndTable();
                }
                ImGui.Unindent(10.0f);

                ImGuiHelpers.ScaledDummy(5.0f);
            }

            ImGui.EndTabItem();
        }
    }
}
