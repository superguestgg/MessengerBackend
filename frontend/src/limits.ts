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
  attachmentsMax: 10, // MessageContent.MaxAttachments
  voiceMaxSeconds: 300, // Attachment.MaxVoiceDurationSeconds
  searchMin: 2, // SearchUsersQuery.MinLength; the max is emailMax
  pageSize: 50,
}
