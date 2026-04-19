using Godot;

namespace STS2RitsuLib.Audio.Internal
{
    /// <summary>
    ///     GDExtension method names on <c>FmodServer</c> (Godot FMOD addon).
    /// </summary>
    internal static class FmodStudioMethodNames
    {
        internal static readonly StringName LoadBank = new("load_bank");
        internal static readonly StringName UnloadBank = new("unload_bank");
        internal static readonly StringName CheckEventPath = new("check_event_path");
        internal static readonly StringName CheckEventGuid = new("check_event_guid");
        internal static readonly StringName CheckBusPath = new("check_bus_path");
        internal static readonly StringName PlayOneShot = new("play_one_shot");
        internal static readonly StringName PlayOneShotWithParams = new("play_one_shot_with_params");
        internal static readonly StringName PlayOneShotUsingGuid = new("play_one_shot_using_guid");
        internal static readonly StringName CreateEventInstance = new("create_event_instance");
        internal static readonly StringName CreateEventInstanceWithGuid = new("create_event_instance_with_guid");
        internal static readonly StringName GetEventFromGuid = new("get_event_from_guid");
        internal static readonly StringName GetEventGuid = new("get_event_guid");
        internal static readonly StringName GetEventPath = new("get_event_path");
        internal static readonly StringName GetAllBuses = new("get_all_buses");
        internal static readonly StringName GetAllBanks = new("get_all_banks");
        internal static readonly StringName GetAllEventDescriptions = new("get_all_event_descriptions");
        internal static readonly StringName GetBus = new("get_bus");
        internal static readonly StringName SetGlobalParameterByName = new("set_global_parameter_by_name");
        internal static readonly StringName GetGlobalParameterByName = new("get_global_parameter_by_name");

        internal static readonly StringName SetGlobalParameterByNameWithLabel =
            new("set_global_parameter_by_name_with_label");

        internal static readonly StringName MuteAllEvents = new("mute_all_events");
        internal static readonly StringName UnmuteAllEvents = new("unmute_all_events");
        internal static readonly StringName PauseAllEvents = new("pause_all_events");
        internal static readonly StringName UnpauseAllEvents = new("unpause_all_events");
        internal static readonly StringName SetSystemDspBufferSize = new("set_system_dsp_buffer_size");
        internal static readonly StringName GetPerformanceData = new("get_performance_data");
        internal static readonly StringName LoadFileAsSound = new("load_file_as_sound");
        internal static readonly StringName LoadFileAsMusic = new("load_file_as_music");
        internal static readonly StringName CreateSoundInstance = new("create_sound_instance");
        internal static readonly StringName UnloadFile = new("unload_file");
        internal static readonly StringName WaitForAllLoads = new("wait_for_all_loads");
        internal static readonly StringName BanksStillLoading = new("banks_still_loading");

        internal static readonly StringName PlayOneShotUsingGuidWithParams =
            new("play_one_shot_using_guid_with_params");
    }
}
