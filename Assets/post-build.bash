#!/bin/bash
set -e

echo "Archiving..."
xcodebuild archive \
  -workspace "$WORKSPACE/App.xcworkspace" \
  -scheme "$TARGET_NAME" \
  -configuration Release \
  -archivePath "$WORKSPACE/build/$TARGET_NAME.xcarchive"

echo "Exporting IPA..."
xcodebuild -exportArchive \
  -archivePath "$WORKSPACE/build/$TARGET_NAME.xcarchive" \
  -exportPath "$WORKSPACE/build/ipa" \
  -exportOptionsPlist "$WORKSPACE/ExportOptions.plist"

IPA_PATH="$WORKSPACE/build/ipa/$TARGET_NAME.ipa"
echo "IPA generated at: $IPA_PATH"

echo "Uploading IPA to App Store Connect..."
xcrun upload-app \
  --file "$IPA_PATH" \
  --apiKey "$APPSTORE_API_KEY" \
  --apiIssuer "$APPSTORE_API_ISSUER"

echo "Upload finished successfully."