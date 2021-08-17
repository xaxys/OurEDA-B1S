#include "stm32f10x.h"
#include "LED.h"
#include "IIC.h"
#include "delay.h"
#include "usart.h"
#include "stdio.h"
/*
Keil: MDK5.10.0.2
MCU:stm32f103c8
Ӳ���ӷ���
GY-39---STM32
SCL---PB6
SDA---PB7
STM32---FT232
TX---RX
RX---TX
���˵��:
�ó������IIC��GY-39���ж�ȡ����,Ȼ�󴮿ڴ�ӡ��������Ϊ115200

ע��
	IICʱ��Ƶ�������40Khz��
	�жϺ���λ��stm32f10x_it.c
	�ºó������û����������븴λstm32
��ϵ��ʽ��
http://shop62474960.taobao.com/?spm=a230r.7195193.1997079397.2.9qa3Ky&v=1
*/

int fputc(int ch, FILE *f)
{
 while (!(USART1->SR & USART_FLAG_TXE));
 USART_SendData(USART1, (unsigned char) ch);// USART1 ���Ի��� USART2 ��
 return (ch);
}
static void NVIC_Configuration(void)
{
  NVIC_InitTypeDef NVIC_X;
  
  /* 4����ռ���ȼ���4����Ӧ���ȼ� */
  NVIC_PriorityGroupConfig(NVIC_PriorityGroup_2);
  /*��ռ���ȼ��ɴ���жϼ���͵��ж�*/
	/*��Ӧ���ȼ����ȼ�ִ��*/
	NVIC_X.NVIC_IRQChannel = USART1_IRQn;//�ж�����
  NVIC_X.NVIC_IRQChannelPreemptionPriority = 0;//��ռ���ȼ�
  NVIC_X.NVIC_IRQChannelSubPriority = 0;//��Ӧ���ȼ�
  NVIC_X.NVIC_IRQChannelCmd = ENABLE;//ʹ���ж���Ӧ
  NVIC_Init(&NVIC_X);
}
typedef struct
{
    uint32_t P;
    uint16_t Temp;
    uint16_t Hum;
    uint16_t Alt;
} bme;

bme Bme={0,0,0,0};

int main(void)
{
	u8  raw_data[13]={0};
	uint16_t data_16[2]={0};
	uint32_t Lux;
	delay_init(72);
	LED_Int(GPIOB,GPIO_Pin_9,RCC_APB2Periph_GPIOB);
	NVIC_Configuration();
	Usart_Int(115200);
	I2C_GPIO_Config();
	delay_ms(100);//�ȴ�ģ���ʼ�����
	while(1)
	{
			if(Single_ReadI2C(0xb6,0x04,raw_data,10))
			{
				Bme.Temp=(raw_data[0]<<8)|raw_data[1];
				data_16[0]=(((uint16_t)raw_data[2])<<8)|raw_data[3];
				data_16[1]=(((uint16_t)raw_data[4])<<8)|raw_data[5];
				Bme.P=(((uint32_t)data_16[0])<<16)|data_16[1];
				Bme.Hum=(raw_data[6]<<8)|raw_data[7];
				Bme.Alt=(raw_data[8]<<8)|raw_data[9];
			}
			if(Single_ReadI2C(0xb6,0x00,raw_data,4))
			data_16[0]=(((uint16_t)raw_data[0])<<8)|raw_data[1];
			data_16[1]=(((uint16_t)raw_data[2])<<8)|raw_data[3];
			Lux=(((uint32_t)data_16[0])<<16)|data_16[1];
			
		  printf("Temp: %.2f  DegC  ",(float)Bme.Temp/100);
		  printf("  P: %.2f  Pa ",(float)Bme.P/100);
			printf("  Hum: %.2f   ",(float)Bme.Hum/100);
		  printf("  Alt: %.2f  m\r\n ",(float)Bme.Alt);
			printf("\r\n Lux: %.2f  lux\r\n ",(float)Lux/100);  
			delay_ms(200);
			
	}		
}
