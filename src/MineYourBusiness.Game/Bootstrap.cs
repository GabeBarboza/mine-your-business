using Godot;
using MineYourBusiness.Domain;

namespace MineYourBusiness;

/// <summary>
/// Composition root for the Godot client.
/// </summary>
public partial class Bootstrap : Control
{
    public override void _Ready()
    {
        Label titleLabel = GetNode<Label>("MarginContainer/Content/Title");
        Label subtitleLabel = GetNode<Label>("MarginContainer/Content/Subtitle");
        Label statusLabel = GetNode<Label>("MarginContainer/Content/Status");

        titleLabel.Text = ProjectMetadata.DisplayName;
        subtitleLabel.Text = ProjectMetadata.Tagline;
        statusLabel.Text = $"Fundação carregada · regras {ProjectMetadata.RulesetVersion}";

        GD.Print($"{ProjectMetadata.DisplayName} {ProjectMetadata.RulesetVersion} started.");
    }
}
