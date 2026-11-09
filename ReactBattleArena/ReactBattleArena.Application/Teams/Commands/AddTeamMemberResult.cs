namespace ReactBattleArena.Application.Teams.Commands;

public enum AddTeamMemberResult
{
    Ok,
    TeamNotFound,
    CharacterNotFound,
    SlotOccupied,
    CharacterAlreadyInTeam,
    TeamFull
}
// Handler tek bool ile “neden olmadı”yı taşıyamaz; controller 404 ile 400’ü ayırsın diye durum kodu döneriz.