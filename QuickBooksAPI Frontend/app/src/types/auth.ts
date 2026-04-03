export interface JwtClaims {
  UserId: string;
  NameIdentifier: string;
  Name: string;
  RealmIds: string[];
}

export interface UserSignUpRequest {
  firstName: string;
  lastName: string;
  username: string;
  email: string;
  password: string;
}

export interface UserLoginRequest {
  email: string;
  password: string;
}

export interface ConnectedCompany {
  id: number;
  qboRealmId: string;
  companyName: string | null;
  connectedAtUtc: string | null;
  isQboConnected: boolean;
}
