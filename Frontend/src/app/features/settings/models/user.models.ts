export interface ManagedUser {
  userId: string;
  employeeId?: string | null;
  employeeName?: string | null;
  email: string;
  firstName: string;
  lastName: string;
  companyId?: string | null;
  roles: string[];
  isActive: boolean;
}

export interface CreateUserRequest {
  companyId: string;
  employeeId: string;
  password: string;
  role: 'Employee' | 'PayrollManager';
}
