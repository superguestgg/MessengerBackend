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
  pageSize: 50,
}
