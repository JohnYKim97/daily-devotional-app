import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ReadingComponent } from './reading.component';

describe('Reading', () => {
  let component: ReadingComponent;
  let fixture: ComponentFixture<ReadingComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ReadingComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(ReadingComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
