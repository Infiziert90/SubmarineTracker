using SubmarineTracker.Data;

namespace SubmarineTracker.Windows.Loot;

public partial class LootWindow
{
    private void VoyageTab()
    {
        if (ImGui.BeginTabItem("Voyage"))
        {
            var existingSubs = Submarines.KnownSubmarines.Values
                                         .SelectMany(fc => fc.Submarines.Select(s => $"{s.Name} ({s.Build.FullIdentifier()})"))
                                         .ToArray();
            if (!existingSubs.Any())
            {
                Helper.NoData();
                ImGui.EndTabItem();
                return;
            }

            var selectedSubmarine = SelectedSubmarine;
            ImGui.Combo("##existingSubs", ref selectedSubmarine, existingSubs, existingSubs.Length);
            if (selectedSubmarine != SelectedSubmarine)
            {
                SelectedSubmarine = selectedSubmarine;
                SelectedVoyage = 0;
            }

            var selectedSub =
                Submarines.KnownSubmarines.Values.SelectMany(fc => fc.Submarines).ToList()[SelectedSubmarine];

            var fc = Submarines.KnownSubmarines.Values.First(fcLoot => fcLoot.SubLoot.Values.Any(loot => loot.Loot.ContainsKey(selectedSub.Return)));
            var submarineLoot = fc.SubLoot.Values.First(loot => loot.Loot.ContainsKey(selectedSub.Return));

            var submarineVoyage = submarineLoot.Loot
                                               .SkipLast(1)
                                               .Select(kv => $"{kv.Value.First().Date}")
                                               .ToArray();
            if (!submarineVoyage.Any())
            {
                ImGui.TextColored(ImGuiColors.ParsedOrange, "Tracking starts when you send your subs on voyage again.");

                ImGui.EndTabItem();
                return;
            }

            ImGui.Combo("##voyageSelection", ref SelectedVoyage, submarineVoyage, submarineVoyage.Length);

            ImGuiHelpers.ScaledDummy(5.0f);

            var loot = submarineLoot.Loot.ToArray()[SelectedVoyage];
            var stats = loot.Value.First();
            if (stats.Valid)
                ImGui.TextUnformatted($"Rank: {stats.Rank} SRF: {stats.Surv}, {stats.Ret}, {stats.Fav}");
            else
                ImGui.TextColored(ImGuiColors.ParsedOrange, "-Legacy Data-");

            ImGuiHelpers.ScaledDummy(5.0f);

            foreach (var detailedLoot in loot.Value)
            {
                var primaryItem = ItemSheet.GetRow(detailedLoot.Primary)!;
                var additionalItem = ItemSheet.GetRow(detailedLoot.Additional)!;

                ImGui.TextUnformatted(Utils.UpperCaseStr(ExplorationSheet.GetRow(detailedLoot.Sector)!.Destination));
                if (ImGui.BeginTable($"##VoyageLootTable", 3))
                {
                    ImGui.TableSetupColumn("##icon", 0, 0.15f);
                    ImGui.TableSetupColumn("##item");
                    ImGui.TableSetupColumn("##amount", 0, 0.3f);

                    ImGui.TableNextColumn();
                    DrawIcon(primaryItem.Icon);
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(Utils.ToStr(primaryItem.Name));
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted($"{detailedLoot.PrimaryCount}");
                    ImGui.TableNextRow();

                    if (detailedLoot.ValidAdditional)
                    {
                        ImGui.TableNextColumn();
                        DrawIcon(additionalItem.Icon);
                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted(Utils.ToStr(additionalItem.Name));
                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted($"{detailedLoot.AdditionalCount}");
                        ImGui.TableNextRow();
                    }
                }

                ImGui.EndTable();

                if (stats.Valid)
                {
                    ImGui.TextUnformatted($"Favor Proc: {Data.Loot.ProcToText(detailedLoot.FavProc)}");
                    ImGui.TextUnformatted($"Retrieval Proc: {Data.Loot.ProcToText(detailedLoot.PrimaryRetProc)}");
                    ImGui.TextUnformatted($"Primary Surv Proc: {Data.Loot.ProcToText(detailedLoot.PrimarySurvProc)}");

                    if (detailedLoot.ValidAdditional)
                        ImGui.TextUnformatted($"Additional Surveillance Proc: {Data.Loot.ProcToText(detailedLoot.AdditionalSurvProc)}");
                }

                ImGuiHelpers.ScaledDummy(5.0f);
            }

            ImGui.EndTabItem();
        }
    }
}
