export interface QuickBooksToken {
  id: number;
  userId: number;
  realmId: string;
  idToken: string;
  accessToken: string;
  refreshToken: string;
  tokenType: string;
  expiresIn: number;
  xRefreshTokenExpiresIn: number;
  createdAt: string;
  updatedAt: string;
}
