export interface AddressDto {
  street: string;
  city: string;
  state: string;
  postalCode: string;
  country: string;
}

export interface CreateOrderRequest {
  shippingAddress: AddressDto;
  billingAddress?: AddressDto;
  notes?: string;
}

export interface OrderItemDto {
  id: string;
  productId: string;
  productName: string;
  sku: string;
  price: number;
  quantity: number;
  discount: number;
  total: number;
}

export interface OrderDto {
  id: string;
  orderNumber: string;
  status: string;
  subTotal: number;
  taxAmount: number;
  shippingCost: number;
  discountAmount: number;
  totalAmount: number;
  shippingAddress?: AddressDto;
  billingAddress?: AddressDto;
  notes?: string;
  createdAt: string;
  updatedAt?: string;
  items: OrderItemDto[];
  totalItems: number;
  paymentId?: string;
  paymentStatus?: string;
  userEmail?: string;
}

export interface OrderResponse {
  success: boolean;
  message?: string | null;
  data?: OrderDto;
  errors?: string[] | null;
}
