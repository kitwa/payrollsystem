export interface EmployeeList {
  id: string;
  employeeNumber: string;
  firstName: string;
  lastName: string;
  email?: string;
  jobTitle?: string;
  department?: string;
  status: number;
  startDate: string;
}

export interface Employee extends EmployeeList {
  companyId: string;
  idNumber: string;
  dateOfBirth: string;
  gender: number;
  phone?: string;
  address?: string;
  taxNumber?: string;
  uifNumber?: string;
  employmentType: number;
  payFrequency: number;
  terminationDate?: string;
  basicSalary: number;
  bankDetails?: BankDetails;
}

export interface BankDetails {
  bankName: string;
  accountNumber: string;
  branchCode: string;
  accountType: string;
}

export interface CreateEmployee {
  companyId: string;
  firstName: string;
  lastName: string;
  idNumber: string;
  dateOfBirth: string;
  gender: number;
  email?: string;
  phone?: string;
  taxNumber?: string;
  employmentType: number;
  payFrequency: number;
  jobTitle?: string;
  department?: string;
  startDate: string;
  basicSalary: number;
}

export interface Department {
  id: string;
  companyId: string;
  name: string;
}
