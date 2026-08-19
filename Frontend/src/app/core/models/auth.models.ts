export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expires: string;
  email: string;
  firstName: string;
  lastName: string;
  roles: string[];
  userId: string;
  companyId?: string | null;
  employeeId?: string | null;
}

export interface RegisterCompanyRequest {
  companyName: string;
  registrationNumber: string;
  taxNumber?: string | null;
  phone?: string | null;
  companyEmail?: string | null;
  adminFirstName: string;
  adminLastName: string;
  adminEmail: string;
  password: string;
}

export interface CurrentUser {
  email: string;
  firstName: string;
  lastName: string;
  roles: string[];
  token: string;
  refreshToken: string;
  userId: string;
  companyId?: string | null;
  employeeId?: string | null;
}
