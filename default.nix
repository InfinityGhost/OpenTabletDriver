{ lib
, buildDotnetModule
, fetchFromGitHub
, fetchurl
, gtk3
, libX11
, libXrandr
, libappindicator
, libevdev
, libnotify
, udev
, copyDesktopItems
, makeDesktopItem
, nixosTests
, wrapGAppsHook
, dpkg
}:

let
  inherit (builtins) baseNameOf;
  inherit (lib) substring;
in buildDotnetModule rec {
  pname = "OpenTabletDriver";
  version = substring 0 8 (baseNameOf src);

  src = ./.;

  debPkg = fetchurl {
    url = "https://github.com/OpenTabletDriver/OpenTabletDriver/releases/download/v${version}/OpenTabletDriver.deb";
    hash = "sha256-zWSJlkn7K/meTycWNTinC0hp0JubF22dJNOJeEIfGtI=";
  };

  dotnetInstallFlags = [ "--framework=net6.0" ];

  projectFile = [ "OpenTabletDriver.Console" "OpenTabletDriver.Daemon" "OpenTabletDriver.UX.Gtk" ];
  nugetDeps = ./deps.nix;

  executables = [ "OpenTabletDriver.Console" "OpenTabletDriver.Daemon" "OpenTabletDriver.UX.Gtk" ];

  nativeBuildInputs = [
    copyDesktopItems
    wrapGAppsHook
    dpkg
  ];

  runtimeDeps = [
    gtk3
    libX11
    libXrandr
    libappindicator
    libevdev
    libnotify
    udev
  ];

  buildInputs = runtimeDeps;

  doCheck = true;
  testProjectFile = "OpenTabletDriver.Tests/OpenTabletDriver.Tests.csproj";

  disabledTests = [
    # Require networking
    "OpenTabletDriver.Tests.PluginRepositoryTest.ExpandRepositoryTarballFork"
    "OpenTabletDriver.Tests.PluginRepositoryTest.ExpandRepositoryTarball"
    # Require networking & unused in Linux build
    "OpenTabletDriver.Tests.UpdaterTests.UpdaterBase_ProperlyChecks_Version_Async"
    "OpenTabletDriver.Tests.UpdaterTests.Updater_PreventsUpdate_WhenAlreadyUpToDate_Async"
    "OpenTabletDriver.Tests.UpdaterTests.Updater_AllowsReupdate_WhenInstallFailed_Async"
    "OpenTabletDriver.Tests.UpdaterTests.Updater_HasUpdateReturnsFalse_During_UpdateInstall_Async"
    "OpenTabletDriver.Tests.UpdaterTests.Updater_HasUpdateReturnsFalse_After_UpdateInstall_Async"
    "OpenTabletDriver.Tests.UpdaterTests.Updater_Prevents_ConcurrentAndConsecutive_Updates_Async"
    "OpenTabletDriver.Tests.UpdaterTests.Updater_ProperlyBackups_BinAndAppDataDirectory_Async"
    # Intended only to be run in continuous integration, unnecessary for functionality
    "OpenTabletDriver.Tests.ConfigurationTest.Configurations_DeviceIdentifier_IsNotConflicting"
    # Depends on processor load
    "OpenTabletDriver.Tests.TimerTests.TimerAccuracy"
  ];

  postFixup = ''
    # Give a more "*nix" name to the binaries
    mv $out/bin/OpenTabletDriver.Console $out/bin/otd
    mv $out/bin/OpenTabletDriver.Daemon $out/bin/otd-daemon
    mv $out/bin/OpenTabletDriver.UX.Gtk $out/bin/otd-gui

    install -Dm644 $src/OpenTabletDriver.UX/Assets/otd.png -t $out/share/pixmaps

    # TODO: Build udev rules from OpenTabletDriver/OpenTabletDriver-udev
  '';

  desktopItems = [
    (makeDesktopItem {
      desktopName = "OpenTabletDriver";
      name = "OpenTabletDriver";
      exec = "otd-gui";
      icon = "otd";
      comment = meta.description;
      categories = [ "Utility" ];
    })
  ];

  meta = with lib; {
    description = "Open source, cross-platform, user-mode tablet driver";
    homepage = "https://github.com/OpenTabletDriver/OpenTabletDriver";
    license = licenses.lgpl3Plus;
    maintainers = with maintainers; [ thiagokokada ];
    platforms = [ "x86_64-linux" "aarch64-linux" ];
    mainProgram = "otd";
  };
}
