using System;


static public class LayerMasks {
    
    // Layer 0: Default
    public const int Default_Index = 0;
    public const int Default_Mask = 1;
    // Layer 1: TransparentFX
    public const int TransparentFX_Index = 1;
    public const int TransparentFX_Mask = 2;
    // Layer 2: Ignore Raycast
    public const int IgnoreRaycast_Index = 2;
    public const int IgnoreRaycast_Mask = 4;
    // Layer 4: Water
    public const int Water_Index = 4;
    public const int Water_Mask = 16;
    // Layer 5: UI
    public const int UI_Index = 5;
    public const int UI_Mask = 32;
    // Layer 6: LabInteract
    public const int LabInteract_Index = 6;
    public const int LabInteract_Mask = 64;
    // Layer 7: DocumentInteract
    public const int DocumentInteract_Index = 7;
    public const int DocumentInteract_Mask = 128;
    // Layer 8: OffscreenReferenceGuide
    public const int OffscreenReferenceGuide_Index = 8;
    public const int OffscreenReferenceGuide_Mask = 256;
    // Layer 9: ReferenceInteract
    public const int ReferenceInteract_Index = 9;
    public const int ReferenceInteract_Mask = 512;
    // Layer 10: DocumentSurface
    public const int DocumentSurface_Index = 10;
    public const int DocumentSurface_Mask = 1024;
    // Layer 15: TopLayer
    public const int TopLayer_Index = 15;
    public const int TopLayer_Mask = 32768;
    // Layer 16: SpaceDome
    public const int SpaceDome_Index = 16;
    public const int SpaceDome_Mask = 65536;
}
static public class SortingLayers {
    
    // Layer Default
    public const int Default = 0;
}
static public class UnityTags {
    
    // Tag Untagged
    public const string Untagged = "Untagged";
    // Tag Respawn
    public const string Respawn = "Respawn";
    // Tag Finish
    public const string Finish = "Finish";
    // Tag EditorOnly
    public const string EditorOnly = "EditorOnly";
    // Tag MainCamera
    public const string MainCamera = "MainCamera";
    // Tag Player
    public const string Player = "Player";
    // Tag GameController
    public const string GameController = "GameController";
}
static public class RenderingLayers {
    
    // Rendering Layer 0: Default
    public const uint Default_Index = 0;
    public const uint Default_Mask = 1;
}