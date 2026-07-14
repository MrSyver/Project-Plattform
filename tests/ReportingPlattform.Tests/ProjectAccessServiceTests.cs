using ReportingPlattform.Core.Domain;
using ReportingPlattform.Core.Services;
using Xunit;

namespace ReportingPlattform.Tests;

public class ProjectAccessServiceTests
{
    private static readonly ProjectAccessService Access = new();
    private static readonly string[] None = Array.Empty<string>();

    private static ProjectSpace SpaceWith(params AccessEntry[] entries)
    {
        var s = new ProjectSpace { Name = "Test", Slug = "test" };
        s.AccessList.AddRange(entries);
        return s;
    }

    [Fact]
    public void Admin_sees_and_edits_everything()
    {
        var u = new UserContext("admin@kunde.de", new[] { "Admin" }, None, CanUseEditor: false);
        var space = SpaceWith(); // leere ACL
        Assert.True(Access.CanView(u, space));
        Assert.True(Access.CanEdit(u, space));
    }

    [Fact]
    public void User_without_acl_entry_sees_nothing()
    {
        var u = new UserContext("nutzer@kunde.de", new[] { "Viewer" }, None, CanUseEditor: false);
        Assert.False(Access.CanView(u, SpaceWith()));
    }

    [Fact]
    public void Acl_email_entry_grants_view()
    {
        var u = new UserContext("nutzer@kunde.de", new[] { "Viewer" }, None, CanUseEditor: false);
        var space = SpaceWith(new AccessEntry
        {
            SubjectType = AccessSubjectType.InternalUser,
            Subject = "NUTZER@kunde.de", // case-insensitive
            Role = ProjectRole.Betrachter,
        });
        Assert.True(Access.CanView(u, space));
        Assert.False(Access.CanEdit(u, space)); // Betrachter + kein Editor
    }

    [Fact]
    public void Security_group_entry_grants_view()
    {
        var u = new UserContext("nutzer@kunde.de", new[] { "Viewer" }, new[] { "grp-123" }, CanUseEditor: false);
        var space = SpaceWith(new AccessEntry
        {
            SubjectType = AccessSubjectType.SecurityGroup,
            Subject = "grp-123",
            Role = ProjectRole.Betrachter,
        });
        Assert.True(Access.CanView(u, space));
    }

    [Fact]
    public void Edit_requires_editor_capability_AND_contributor_role()
    {
        var entry = new AccessEntry
        {
            SubjectType = AccessSubjectType.InternalUser,
            Subject = "editor@kunde.de",
            Role = ProjectRole.Beitragender,
        };

        var withEditor = new UserContext("editor@kunde.de", new[] { "Viewer" }, None, CanUseEditor: true);
        var withoutEditor = new UserContext("editor@kunde.de", new[] { "Viewer" }, None, CanUseEditor: false);

        Assert.True(Access.CanEdit(withEditor, SpaceWith(entry)));
        Assert.False(Access.CanEdit(withoutEditor, SpaceWith(entry)));
    }

    [Fact]
    public void External_guest_entry_works_like_internal_user()
    {
        var u = new UserContext("gast@extern.com", new[] { "Viewer" }, None, CanUseEditor: false);
        var space = SpaceWith(new AccessEntry
        {
            SubjectType = AccessSubjectType.ExternalGuest,
            Subject = "gast@extern.com",
            Role = ProjectRole.Betrachter,
        });
        Assert.True(Access.CanView(u, space));
    }

    [Fact]
    public void Manage_members_requires_admin_or_owner()
    {
        var ownerEntry = new AccessEntry { SubjectType = AccessSubjectType.InternalUser, Subject = "owner@kunde.de", Role = ProjectRole.Owner };
        var contribEntry = new AccessEntry { SubjectType = AccessSubjectType.InternalUser, Subject = "beitrag@kunde.de", Role = ProjectRole.Beitragender };
        var space = SpaceWith(ownerEntry, contribEntry);

        var admin = new UserContext("admin@kunde.de", new[] { "Admin" }, None, CanUseEditor: false);
        var owner = new UserContext("owner@kunde.de", new[] { "Viewer" }, None, CanUseEditor: true);
        var contributor = new UserContext("beitrag@kunde.de", new[] { "Viewer" }, None, CanUseEditor: true);

        Assert.True(Access.CanManageMembers(admin, space));
        Assert.True(Access.CanManageMembers(owner, space));
        Assert.False(Access.CanManageMembers(contributor, space));
    }

    [Fact]
    public void Highest_role_wins_across_entries()
    {
        var u = new UserContext("x@kunde.de", new[] { "Viewer" }, new[] { "grp-1" }, CanUseEditor: true);
        var space = SpaceWith(
            new AccessEntry { SubjectType = AccessSubjectType.SecurityGroup, Subject = "grp-1", Role = ProjectRole.Betrachter },
            new AccessEntry { SubjectType = AccessSubjectType.InternalUser, Subject = "x@kunde.de", Role = ProjectRole.Owner });
        Assert.Equal(ProjectRole.Owner, Access.EffectiveRole(u, space));
        Assert.True(Access.CanEdit(u, space));
    }
}
