#include "stm32f10x.h"
#include "delay.h"
#include "usart.h"
#include "string.h"
 #include "LED.h"
 #include "stdio.h"
 /*
 Keil: MDK5.10.0.2
MCU:stm32f103c8
Ӳ���ӷ���
GY-39---STM32
1��GY-39_RX---STM32_TX,STM32��λ������A5 82 27 ��ģ��
2��STM32_TX---FT232,STM32�������ϴ�����λ��
3��GY-39_TX---STM32_RX������ģ������
���˵��:
�ó�����ô��ڷ�ʽ��ȡģ�����ݣ�������9600

ע��ģ�鲨������͸ó�������һ��Ϊ9600���жϺ���λ��stm32f10x_it.c
��ϵ��ʽ��
http://shop62474960.taobao.com/?spm=a230r.7195193.1997079397.2.9qa3Ky&v=1
*/
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
void send_com(u8 data)
{
	u8 bytes[3]={0};
	bytes[0]=0xa5;
	bytes[1]=data;//�����ֽ�
	USART_Send(bytes,3);//����֡ͷ�������ֽڡ�У���
}
typedef struct
{
    uint32_t P;
    uint16_t Temp;
    uint16_t Hum;
    uint16_t Alt;
} bme;

int fputc(int ch, FILE *f)
{
 while (!(USART1->SR & USART_FLAG_TXE));
 USART_SendData(USART1, (unsigned char) ch);// USART1 ���Ի��� USART2 ��
 return (ch);
}
int main(void)
{
  u8 sum=0,i=0;
	int16_t data=0;
	uint16_t data_16[2]={0};
	bme Bme={0,0,0,0};
	delay_init(72);
	NVIC_Configuration();
	Usart_Int(9600);
	delay_ms(100);//�ȴ�ģ���ʼ�����
	send_com(0x82);//���Ͷ���ѹ��ʪ��ָ��
	while(1)
	{
		if(Receive_ok)//���ڽ������
		{
			for(sum=0,i=0;i<(raw_data[3]+4);i++)//rgb_data[3]=3
			sum+=raw_data[i];
			if(sum==raw_data[i])//У����ж�
			{
				Bme.Temp=(raw_data[4]<<8)|raw_data[5];
				data_16[0]=(((uint16_t)raw_data[6])<<8)|raw_data[7];
				data_16[1]=(((uint16_t)raw_data[8])<<8)|raw_data[9];
				Bme.P=(((uint32_t)data_16[0])<<16)|data_16[1];
        Bme.Hum=(raw_data[10]<<8)|raw_data[11];
        Bme.Alt=(raw_data[12]<<8)|raw_data[13]; 
				send_3out(&raw_data[4],10,0x45);//�ϴ�����λ��
				
//			  printf("Temp: %.2f  DegC  ",(float)Bme.Temp/100);
//		    printf("  P: %.2f  Pa ",(float)Bme.P/100);
//			  printf("  Hum: %.2f   ",(float)Bme.Hum/100);
//		    printf("  Alt: %.2f  m\r\n ",(float)Bme.Alt);
			}
			Receive_ok=0;//����������ϱ�־
		}
	}
}
