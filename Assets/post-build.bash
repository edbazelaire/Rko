#!/bin/bash
set -e

echo "Uploading IPA to App Store Connect..."

IPA_PATH="$WORKSPACE/.build/last/$TARGET_NAME/build.ipa"

if [ ! -f "$IPA_PATH" ]; then
  echo "❌ IPA not found at $IPA_PATH"
  exit 1
fi

xcrun altool --upload-app \
  -t ios \
  -f "$IPA_PATH" \
  -u "$ITUNES_USERNAME" \
  -p "$ITUNES_PASSWORD"

echo "✅ Upload finished"