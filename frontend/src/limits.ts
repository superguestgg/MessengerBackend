// Mirrors the backend's domain constants (Swagger doesn't carry them): keep in sync.
export const limits = {
  emailMax: 254, // Email.MaxLength
  passwordMin: 8,
  passwordMax: 128,
  displayNameMax: 64, // DisplayName.MaxLength
  bioMax: 500, // UserProfile.BioMaxLength
  tokenNameMax: 64, // AccessTokenName.MaxLength
  chatTitleMax: 128, // ChatTitle.MaxLength
  messageMax: 4096, // MessageText.MaxLength
  fileMaxBytes: 1024 * 1024, // StoredFile.MaxSize
  imageMaxBytes: 10 * 1024 * 1024, // Client only: a larger photo is compressed to fileMaxBytes before upload
  attachmentsMax: 10, // MessageContent.MaxAttachments
  voiceMaxSeconds: 300, // Attachment.MaxVoiceDurationSeconds
  geoAccuracyMaxMeters: 100_000, // GeoPoint.MaxAccuracyMeters
  // LiveLocation.MinPeriodSeconds .. LiveLocation.MaxPeriodSeconds
  livePeriodsSeconds: [15 * 60, 60 * 60, 8 * 60 * 60],
  searchMin: 2, // SearchUsersQuery.MinLength; the max is emailMax
  pageSize: 50,
}
