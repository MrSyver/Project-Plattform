namespace ReportingPlattform.Core.Domain;

/// <summary>Plattformweite Fähigkeit (App Role). Siehe Technische Doku § 2.4.</summary>
public enum PlatformRole { Viewer, Editor, Admin, Auditor }

/// <summary>Rolle innerhalb eines Projektraums (ACL-Eintrag).</summary>
public enum ProjectRole { Betrachter, Beitragender, Owner }

/// <summary>Art des Subjekts in der Projektraum-ACL.</summary>
public enum AccessSubjectType { SecurityGroup, InternalUser, ExternalGuest }

/// <summary>Bausteintypen des In-App-Editors.</summary>
public enum BlockType { Text, RichText, PowerBiReport, FileLibrary, Video, Form }

/// <summary>Entwurf/Veröffentlichen-Status einer Seite (ADR-018).</summary>
public enum PublishState { Draft, Published }
