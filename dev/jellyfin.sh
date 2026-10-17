#!/usr/bin/env bash
# Local Jellyfin 12.2 for trying the plugin out.
#
#   dev/jellyfin.sh up       start Jellyfin on http://localhost:8096
#   dev/jellyfin.sh deploy   build the plugin, install it, restart Jellyfin
#   dev/jellyfin.sh logs     follow the server log
#   dev/jellyfin.sh down     stop and remove the container (data in dev/ is kept)
#
# Uses podman by default; set CONTAINER_ENGINE=docker to use Docker instead.
set -euo pipefail

ENGINE="${CONTAINER_ENGINE:-podman}"
IMAGE="docker.io/jellyfin/jellyfin:12.2"
NAME="tubedlfin-dev"
PORT="${JELLYFIN_PORT:-8096}"

DEV_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_DIR="$(dirname "$DEV_DIR")"
PROJECT="$REPO_DIR/src/Jellyfin.Plugin.TubeDLFin/Jellyfin.Plugin.TubeDLFin.csproj"

plugin_version() {
    sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' "$REPO_DIR/Directory.Build.props" | head -n 1
}

cmd_up() {
    mkdir -p "$DEV_DIR"/config "$DEV_DIR"/cache "$DEV_DIR"/media/{movies,tv,music,musicvideos,homevideos}

    if "$ENGINE" container exists "$NAME" 2>/dev/null || "$ENGINE" inspect "$NAME" >/dev/null 2>&1; then
        "$ENGINE" start "$NAME" >/dev/null
    else
        # :Z relabels the folders for SELinux (Fedora); media is read-write on purpose.
        "$ENGINE" run -d --name "$NAME" \
            -p "$PORT:8096" \
            -v "$DEV_DIR/config:/config:Z" \
            -v "$DEV_DIR/cache:/cache:Z" \
            -v "$DEV_DIR/media:/media:Z" \
            "$IMAGE" >/dev/null
    fi

    echo "Jellyfin: http://localhost:$PORT"
}

cmd_deploy() {
    local version target
    version="$(plugin_version)"
    target="$DEV_DIR/config/plugins/TubeDLFin_$version"

    # Remove other installed versions so Jellyfin loads only this build.
    rm -rf "$DEV_DIR"/config/plugins/TubeDLFin_*
    mkdir -p "$target"

    dotnet publish "$PROJECT" --configuration Debug --output "$target" --nologo -v quiet
    # Jellyfin provides its own assemblies; keep only the plugin.
    find "$target" -type f ! -name 'Jellyfin.Plugin.TubeDLFin.*' -delete

    echo "Installed TubeDLFin $version into $target"

    if "$ENGINE" inspect "$NAME" >/dev/null 2>&1; then
        "$ENGINE" restart "$NAME" >/dev/null
        echo "Restarted $NAME"
    else
        cmd_up
    fi
}

cmd_logs() {
    "$ENGINE" logs -f "$NAME"
}

cmd_down() {
    "$ENGINE" rm -f "$NAME" >/dev/null
    echo "Removed $NAME (dev/config and dev/media are kept)"
}

case "${1:-}" in
    up) cmd_up ;;
    deploy) cmd_deploy ;;
    logs) cmd_logs ;;
    down) cmd_down ;;
    *)
        sed -n '2,9p' "${BASH_SOURCE[0]}" | sed 's/^# \{0,1\}//'
        exit 1
        ;;
esac
